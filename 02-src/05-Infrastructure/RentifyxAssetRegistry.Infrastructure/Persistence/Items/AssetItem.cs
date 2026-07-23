using Amazon.DynamoDBv2.DataModel;

namespace RentifyxAssetRegistry.Infrastructure.Persistence.Items;

/// <summary>
/// Storage-shape POCO for the Asset item type in the single-table design. Attributes describe
/// persistence shape only — mapping to/from <c>AssetEntity</c> is done by hand in
/// <c>AssetDynamoDbMapper</c>.
/// </summary>
[DynamoDBTable("AssetRegistry")]
public sealed class AssetItem
{
    [DynamoDBHashKey(DynamoDbKeys.Pk)]
    public string Pk { get; set; } = string.Empty;

    [DynamoDBRangeKey(DynamoDbKeys.Sk)]
    public string Sk { get; set; } = string.Empty;

    [DynamoDBProperty(DynamoDbKeys.Type)]
    public string Type { get; set; } = DynamoDbKeys.AssetType;

    [DynamoDBGlobalSecondaryIndexHashKey(DynamoDbKeys.Gsi1IndexName, AttributeName = DynamoDbKeys.Gsi1Pk)]
    public string Gsi1Pk { get; set; } = string.Empty;

    [DynamoDBGlobalSecondaryIndexRangeKey(DynamoDbKeys.Gsi1IndexName, AttributeName = DynamoDbKeys.Gsi1Sk)]
    public string Gsi1Sk { get; set; } = string.Empty;

    [DynamoDBGlobalSecondaryIndexHashKey(DynamoDbKeys.Gsi2IndexName, AttributeName = DynamoDbKeys.Gsi2Pk)]
    public string Gsi2Pk { get; set; } = string.Empty;

    [DynamoDBGlobalSecondaryIndexRangeKey(DynamoDbKeys.Gsi2IndexName, AttributeName = DynamoDbKeys.Gsi2Sk)]
    public string Gsi2Sk { get; set; } = string.Empty;

    [DynamoDBGlobalSecondaryIndexHashKey(DynamoDbKeys.Gsi3IndexName, AttributeName = DynamoDbKeys.Gsi3Pk)]
    public string Gsi3Pk { get; set; } = string.Empty;

    [DynamoDBGlobalSecondaryIndexRangeKey(DynamoDbKeys.Gsi3IndexName, AttributeName = DynamoDbKeys.Gsi3Sk)]
    public string Gsi3Sk { get; set; } = string.Empty;

    [DynamoDBGlobalSecondaryIndexHashKey(DynamoDbKeys.Gsi4IndexName, AttributeName = DynamoDbKeys.Gsi4Pk)]
    public string Gsi4Pk { get; set; } = string.Empty;

    [DynamoDBGlobalSecondaryIndexRangeKey(DynamoDbKeys.Gsi4IndexName, AttributeName = DynamoDbKeys.Gsi4Sk)]
    public string Gsi4Sk { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string Id { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string OwnerId { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string Title { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string Description { get; set; } = string.Empty;

    [DynamoDBProperty]
    public decimal PriceAmount { get; set; }

    [DynamoDBProperty]
    public string PriceCurrency { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string CategoryId { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string Status { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string IdempotencyKey { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string CreatedAt { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string? UpdatedAt { get; set; }
}
