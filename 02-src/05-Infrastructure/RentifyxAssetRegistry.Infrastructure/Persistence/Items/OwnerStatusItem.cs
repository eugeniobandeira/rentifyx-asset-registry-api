using Amazon.DynamoDBv2.DataModel;

namespace RentifyxAssetRegistry.Infrastructure.Persistence.Items;

[DynamoDBTable("AssetRegistry")]
public sealed class OwnerStatusItem
{
    [DynamoDBHashKey(DynamoDbKeys.Pk)]
    public string Pk { get; set; } = string.Empty;

    [DynamoDBRangeKey(DynamoDbKeys.Sk)]
    public string Sk { get; set; } = string.Empty;

    [DynamoDBProperty(DynamoDbKeys.Type)]
    public string Type { get; set; } = DynamoDbKeys.OwnerStatusType;

    [DynamoDBProperty]
    public string OwnerId { get; set; } = string.Empty;

    [DynamoDBProperty]
    public bool IsActive { get; set; }

    [DynamoDBProperty]
    public string Reason { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string UpdatedAt { get; set; } = string.Empty;
}
