using Amazon.DynamoDBv2.DataModel;

namespace RentifyxAssetRegistry.Infrastructure.Persistence.Items;

/// <summary>
/// Storage-shape POCO for the Outbox item type. Mapping to/from domain events is done by hand in
/// <c>OutboxDynamoDbMapper</c>.
/// </summary>
[DynamoDBTable("AssetRegistry")]
public sealed class OutboxItem
{
    [DynamoDBHashKey(DynamoDbKeys.Pk)]
    public string Pk { get; set; } = string.Empty;

    [DynamoDBRangeKey(DynamoDbKeys.Sk)]
    public string Sk { get; set; } = string.Empty;

    [DynamoDBProperty(DynamoDbKeys.Type)]
    public string Type { get; set; } = DynamoDbKeys.OutboxType;

    [DynamoDBGlobalSecondaryIndexHashKey(DynamoDbKeys.Gsi1IndexName, AttributeName = DynamoDbKeys.Gsi1Pk)]
    public string? Gsi1Pk { get; set; }

    [DynamoDBGlobalSecondaryIndexRangeKey(DynamoDbKeys.Gsi1IndexName, AttributeName = DynamoDbKeys.Gsi1Sk)]
    public string? Gsi1Sk { get; set; }

    [DynamoDBProperty]
    public string Id { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string EventType { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string Payload { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string Status { get; set; } = string.Empty;

    [DynamoDBProperty]
    public int RetryCount { get; set; }

    [DynamoDBProperty]
    public string CreatedAtUtc { get; set; } = string.Empty;
}
