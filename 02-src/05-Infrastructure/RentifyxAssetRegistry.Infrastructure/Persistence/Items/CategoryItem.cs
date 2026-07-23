using Amazon.DynamoDBv2.DataModel;

namespace RentifyxAssetRegistry.Infrastructure.Persistence.Items;

/// <summary>
/// Storage-shape POCO for the Category item type. Mapping to/from <c>CategoryEntity</c> is done
/// by hand in <c>CategoryDynamoDbMapper</c>.
/// </summary>
[DynamoDBTable("AssetRegistry")]
public sealed class CategoryItem
{
    [DynamoDBHashKey(DynamoDbKeys.Pk)]
    public string Pk { get; set; } = string.Empty;

    [DynamoDBRangeKey(DynamoDbKeys.Sk)]
    public string Sk { get; set; } = string.Empty;

    [DynamoDBProperty(DynamoDbKeys.Type)]
    public string Type { get; set; } = DynamoDbKeys.CategoryType;

    [DynamoDBGlobalSecondaryIndexHashKey(DynamoDbKeys.Gsi1IndexName, AttributeName = DynamoDbKeys.Gsi1Pk)]
    public string Gsi1Pk { get; set; } = string.Empty;

    [DynamoDBGlobalSecondaryIndexRangeKey(DynamoDbKeys.Gsi1IndexName, AttributeName = DynamoDbKeys.Gsi1Sk)]
    public string Gsi1Sk { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string Id { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string Name { get; set; } = string.Empty;

    [DynamoDBProperty]
    public string? ParentCategoryId { get; set; }

    [DynamoDBProperty]
    public int Depth { get; set; }
}
