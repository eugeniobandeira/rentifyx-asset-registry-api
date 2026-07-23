using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Interfaces.Category;
using RentifyxAssetRegistry.Infrastructure.Configuration;
using RentifyxAssetRegistry.Infrastructure.Persistence.Items;
using RentifyxAssetRegistry.Infrastructure.Persistence.Mappers;

namespace RentifyxAssetRegistry.Infrastructure.Persistence;

/// <summary>
/// DynamoDB implementation of <see cref="ICategoryRepository"/> against the single-table design
/// described in .specs/features/e04-f10-dynamodb-repository/design.md. CategoryEntity raises no
/// domain events, so SaveAsync is always a plain upsert — no transactional outbox write needed.
/// </summary>
public sealed class DynamoDbCategoryRepository(
    IAmazonDynamoDB client,
    IDynamoDBContext context,
    DynamoDbOptions options) : ICategoryRepository
{
    public async Task<CategoryEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        string key = DynamoDbKeys.CategoryKey(id);
        CategoryItem? item = await context.LoadAsync<CategoryItem>(
            key,
            key,
            new LoadConfig { OverrideTableName = options.TableName },
            ct);

        return item is null ? null : CategoryDynamoDbMapper.ToEntity(item);
    }

    public async Task<IReadOnlyList<CategoryEntity>> GetAllAsync(CancellationToken ct = default)
    {
        List<CategoryEntity> results = [];
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
                    [":pk"] = new AttributeValue(DynamoDbKeys.CategoryListValue)
                },
                ExclusiveStartKey = exclusiveStartKey
            };

            QueryResponse response = await client.QueryAsync(request, ct);

            results.AddRange(response.Items.Select(
                i => CategoryDynamoDbMapper.ToEntity(CategoryDynamoDbMapper.FromAttributeMap(i))));

            exclusiveStartKey = response.LastEvaluatedKey is { Count: > 0 } ? response.LastEvaluatedKey : null;
        }
        while (exclusiveStartKey is not null);

        return results;
    }

    public async Task SaveAsync(CategoryEntity entity, CancellationToken ct = default)
    {
        CategoryItem item = CategoryDynamoDbMapper.ToItem(entity);

        await context.SaveAsync(item, new SaveConfig { OverrideTableName = options.TableName }, ct);
    }
}
