using System.Globalization;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using RentifyxAssetRegistry.Domain.Common;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Enums;
using RentifyxAssetRegistry.Domain.Filters.Assets;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;
using RentifyxAssetRegistry.Infrastructure.Configuration;
using RentifyxAssetRegistry.Infrastructure.Persistence.Items;
using RentifyxAssetRegistry.Infrastructure.Persistence.Mappers;

namespace RentifyxAssetRegistry.Infrastructure.Persistence;

/// <summary>
/// DynamoDB implementation of <see cref="IAssetRepository"/> against the single-table design
/// described in .specs/features/e04-f10-dynamodb-repository/design.md.
/// </summary>
public sealed class DynamoDbAssetRepository(
    IAmazonDynamoDB client,
    IDynamoDBContext context,
    DynamoDbOptions options) : IAssetRepository
{
    private const string PriceAmountAttribute = "PriceAmount";
    private const string StatusAttribute = "Status";
    private const string TitleAttribute = "Title";

    public async Task<AssetEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        string key = DynamoDbKeys.AssetKey(id);
        AssetItem? item = await context.LoadAsync<AssetItem>(
            key,
            key,
            new LoadConfig { OverrideTableName = options.TableName },
            ct);

        return item is null ? null : AssetDynamoDbMapper.ToEntity(item);
    }

    public async Task SaveAsync(AssetEntity entity, CancellationToken ct = default)
    {
        AssetItem item = AssetDynamoDbMapper.ToItem(entity);

        if (entity.DomainEvents.Count == 0)
        {
            await context.SaveAsync(item, new SaveConfig { OverrideTableName = options.TableName }, ct);
            return;
        }

        if (entity.DomainEvents.Count + 1 > 100)
        {
            throw new InvalidOperationException(
                "Cannot persist an asset with enough pending domain events to exceed DynamoDB's " +
                "100-item TransactWriteItems limit (asset item + one outbox entry per event).");
        }

        List<TransactWriteItem> transactItems = new(entity.DomainEvents.Count + 1)
        {
            new TransactWriteItem
            {
                Put = new Put
                {
                    TableName = options.TableName,
                    Item = AssetDynamoDbMapper.ToAttributeMap(item)
                }
            }
        };

        foreach (IDomainEvent domainEvent in entity.DomainEvents)
            transactItems.Add(OutboxDynamoDbMapper.ToTransactPut(domainEvent, options.TableName));

        await client.TransactWriteItemsAsync(
            new TransactWriteItemsRequest { TransactItems = transactItems },
            ct);

        entity.ClearDomainEvents();
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        string key = DynamoDbKeys.AssetKey(id);
        string statusName = AssetStatus.Archived.ToString();

        await client.UpdateItemAsync(
            new UpdateItemRequest
            {
                TableName = options.TableName,
                Key = new Dictionary<string, AttributeValue>
                {
                    [DynamoDbKeys.Pk] = new AttributeValue(key),
                    [DynamoDbKeys.Sk] = new AttributeValue(key)
                },
                UpdateExpression = "SET #status = :status, #gsi4pk = :gsi4pk",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#status"] = StatusAttribute,
                    ["#gsi4pk"] = DynamoDbKeys.Gsi4Pk
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":status"] = new AttributeValue(statusName),
                    [":gsi4pk"] = new AttributeValue(DynamoDbKeys.StatusKey(statusName))
                }
            },
            ct);
    }

    public async Task<IReadOnlyList<AssetEntity>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default)
    {
        List<AssetEntity> results = [];
        Dictionary<string, AttributeValue>? exclusiveStartKey = null;

        do
        {
            QueryRequest request = new()
            {
                TableName = options.TableName,
                IndexName = DynamoDbKeys.Gsi1IndexName,
                KeyConditionExpression = "#gsi1pk = :pk",
                ExpressionAttributeNames = new Dictionary<string, string> { ["#gsi1pk"] = DynamoDbKeys.Gsi1Pk },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":pk"] = new AttributeValue(DynamoDbKeys.OwnerKey(ownerId))
                },
                ExclusiveStartKey = exclusiveStartKey
            };

            QueryResponse response = await client.QueryAsync(request, ct);

            results.AddRange(response.Items.Select(
                i => AssetDynamoDbMapper.ToEntity(AssetDynamoDbMapper.FromAttributeMap(i))));

            exclusiveStartKey = response.LastEvaluatedKey is { Count: > 0 } ? response.LastEvaluatedKey : null;
        }
        while (exclusiveStartKey is not null);

        return results;
    }

    public async Task<AssetEntity?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default)
    {
        QueryRequest request = new()
        {
            TableName = options.TableName,
            IndexName = DynamoDbKeys.Gsi3IndexName,
            KeyConditionExpression = "#gsi3pk = :pk",
            ExpressionAttributeNames = new Dictionary<string, string> { ["#gsi3pk"] = DynamoDbKeys.Gsi3Pk },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue(DynamoDbKeys.IdempotencyKey(idempotencyKey))
            },
            Limit = 1
        };

        QueryResponse response = await client.QueryAsync(request, ct);

        return response.Items.Count == 0
            ? null
            : AssetDynamoDbMapper.ToEntity(AssetDynamoDbMapper.FromAttributeMap(response.Items[0]));
    }

    public async Task<CursorPagedResult<AssetEntity>> SearchAsync(AssetSearchFilter filter, CancellationToken ct = default)
    {
        string statusName = filter.Status.ToString();
        Dictionary<string, string> names = [];
        Dictionary<string, AttributeValue> values = [];
        List<string> filterParts = [];
        string indexName;

        if (filter.CategoryId is { } categoryId)
        {
            indexName = DynamoDbKeys.Gsi2IndexName;
            names["#pk"] = DynamoDbKeys.Gsi2Pk;
            values[":pk"] = new AttributeValue(DynamoDbKeys.CategoryPartitionKey(categoryId));

            names["#status"] = StatusAttribute;
            values[":status"] = new AttributeValue(statusName);
            filterParts.Add("#status = :status");
        }
        else
        {
            indexName = DynamoDbKeys.Gsi4IndexName;
            names["#pk"] = DynamoDbKeys.Gsi4Pk;
            values[":pk"] = new AttributeValue(DynamoDbKeys.StatusKey(statusName));
        }

        AppendPriceFilter(filter, names, values, filterParts);

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            names["#title"] = TitleAttribute;
            values[":keyword"] = new AttributeValue(filter.Keyword);
            filterParts.Add("contains(#title, :keyword)");
        }

        QueryRequest request = new()
        {
            TableName = options.TableName,
            IndexName = indexName,
            KeyConditionExpression = "#pk = :pk",
            ExpressionAttributeNames = names,
            ExpressionAttributeValues = values,
            Limit = filter.PageSize
        };

        if (filterParts.Count > 0)
            request.FilterExpression = string.Join(" AND ", filterParts);

        if (!string.IsNullOrWhiteSpace(filter.NextPageToken))
            request.ExclusiveStartKey = PageTokenCodec.Decode(filter.NextPageToken);

        QueryResponse response = await client.QueryAsync(request, ct);

        List<AssetEntity> items = response.Items
            .Select(i => AssetDynamoDbMapper.ToEntity(AssetDynamoDbMapper.FromAttributeMap(i)))
            .ToList();

        string? nextPageToken = response.LastEvaluatedKey is { Count: > 0 }
            ? PageTokenCodec.Encode(response.LastEvaluatedKey)
            : null;

        return new CursorPagedResult<AssetEntity>(items, nextPageToken);
    }

    private static void AppendPriceFilter(
        AssetSearchFilter filter,
        Dictionary<string, string> names,
        Dictionary<string, AttributeValue> values,
        List<string> filterParts)
    {
        if (filter.MinPrice is null && filter.MaxPrice is null)
            return;

        names["#price"] = PriceAmountAttribute;

        if (filter.MinPrice is { } min && filter.MaxPrice is { } max)
        {
            values[":minPrice"] = new AttributeValue { N = min.ToString(CultureInfo.InvariantCulture) };
            values[":maxPrice"] = new AttributeValue { N = max.ToString(CultureInfo.InvariantCulture) };
            filterParts.Add("#price BETWEEN :minPrice AND :maxPrice");
        }
        else if (filter.MinPrice is { } minOnly)
        {
            values[":minPrice"] = new AttributeValue { N = minOnly.ToString(CultureInfo.InvariantCulture) };
            filterParts.Add("#price >= :minPrice");
        }
        else if (filter.MaxPrice is { } maxOnly)
        {
            values[":maxPrice"] = new AttributeValue { N = maxOnly.ToString(CultureInfo.InvariantCulture) };
            filterParts.Add("#price <= :maxPrice");
        }
    }

}
