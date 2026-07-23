using System.Globalization;
using System.Text.Json;
using Amazon.DynamoDBv2.Model;
using RentifyxAssetRegistry.Domain.Common;
using RentifyxAssetRegistry.Infrastructure.Persistence.Items;

namespace RentifyxAssetRegistry.Infrastructure.Persistence.Mappers;

/// <summary>
/// Hand-written mapping between raised <see cref="IDomainEvent"/>s and the Outbox item shape, plus
/// the low-level <see cref="TransactWriteItem"/> builder used by the transactional Save path.
/// </summary>
public static class OutboxDynamoDbMapper
{
    public const string OutboxStatusPending = "Pending";
    public const string OutboxStatusPublished = "Published";
    public const string OutboxStatusFailed = "Failed";

    private const string IdAttribute = "Id";
    private const string EventTypeAttribute = "EventType";
    private const string PayloadAttribute = "Payload";
    private const string StatusAttribute = "Status";
    private const string RetryCountAttribute = "RetryCount";
    private const string CreatedAtUtcAttribute = "CreatedAtUtc";

    public static OutboxItem ToItem(IDomainEvent domainEvent)
    {
        Guid id = Guid.NewGuid();
        string outboxKey = DynamoDbKeys.OutboxKey(id);
        DateTime createdAtUtc = domainEvent.OccurredAt;

        return new OutboxItem
        {
            Pk = outboxKey,
            Sk = outboxKey,
            Type = DynamoDbKeys.OutboxType,
            Gsi1Pk = DynamoDbKeys.OutboxStatusKey(OutboxStatusPending),
            Gsi1Sk = DynamoDbKeys.OutboxSortKey(createdAtUtc, id),
            Id = id.ToString(),
            EventType = domainEvent.GetType().Name,
            Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
            Status = OutboxStatusPending,
            RetryCount = 0,
            CreatedAtUtc = createdAtUtc.ToString("o", CultureInfo.InvariantCulture)
        };
    }

    public static TransactWriteItem ToTransactPut(IDomainEvent domainEvent, string tableName)
        => ToTransactPut(ToItem(domainEvent), tableName);

    public static TransactWriteItem ToTransactPut(OutboxItem item, string tableName)
    {
        return new TransactWriteItem
        {
            Put = new Put
            {
                TableName = tableName,
                Item = ToAttributeMap(item)
            }
        };
    }

    public static Dictionary<string, AttributeValue> ToAttributeMap(OutboxItem item)
    {
        Dictionary<string, AttributeValue> map = new()
        {
            [DynamoDbKeys.Pk] = new AttributeValue(item.Pk),
            [DynamoDbKeys.Sk] = new AttributeValue(item.Sk),
            [DynamoDbKeys.Type] = new AttributeValue(item.Type),
            [IdAttribute] = new AttributeValue(item.Id),
            [EventTypeAttribute] = new AttributeValue(item.EventType),
            [PayloadAttribute] = new AttributeValue(item.Payload),
            [StatusAttribute] = new AttributeValue(item.Status),
            [RetryCountAttribute] = new AttributeValue { N = item.RetryCount.ToString(CultureInfo.InvariantCulture) },
            [CreatedAtUtcAttribute] = new AttributeValue(item.CreatedAtUtc)
        };

        if (!string.IsNullOrEmpty(item.Gsi1Pk))
            map[DynamoDbKeys.Gsi1Pk] = new AttributeValue(item.Gsi1Pk);

        if (!string.IsNullOrEmpty(item.Gsi1Sk))
            map[DynamoDbKeys.Gsi1Sk] = new AttributeValue(item.Gsi1Sk);

        return map;
    }

    public static OutboxItem FromAttributeMap(Dictionary<string, AttributeValue> map)
    {
        return new OutboxItem
        {
            Pk = map[DynamoDbKeys.Pk].S,
            Sk = map[DynamoDbKeys.Sk].S,
            Type = map[DynamoDbKeys.Type].S,
            Gsi1Pk = map.TryGetValue(DynamoDbKeys.Gsi1Pk, out AttributeValue? gsi1Pk) ? gsi1Pk.S : null,
            Gsi1Sk = map.TryGetValue(DynamoDbKeys.Gsi1Sk, out AttributeValue? gsi1Sk) ? gsi1Sk.S : null,
            Id = map[IdAttribute].S,
            EventType = map[EventTypeAttribute].S,
            Payload = map[PayloadAttribute].S,
            Status = map[StatusAttribute].S,
            RetryCount = int.Parse(map[RetryCountAttribute].N, CultureInfo.InvariantCulture),
            CreatedAtUtc = map[CreatedAtUtcAttribute].S
        };
    }
}
