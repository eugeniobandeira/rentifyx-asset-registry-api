using System.Globalization;
using Amazon.DynamoDBv2.Model;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Infrastructure.Persistence.Items;

namespace RentifyxAssetRegistry.Infrastructure.Persistence.Mappers;

/// <summary>
/// Hand-written Domain &lt;-&gt; DynamoDB mapping for <see cref="CategoryEntity"/>.
/// </summary>
public static class CategoryDynamoDbMapper
{
    private const string IdAttribute = "Id";
    private const string NameAttribute = "Name";
    private const string ParentCategoryIdAttribute = "ParentCategoryId";
    private const string DepthAttribute = "Depth";

    public static CategoryItem ToItem(CategoryEntity entity)
    {
        string categoryKey = DynamoDbKeys.CategoryKey(entity.Id);

        return new CategoryItem
        {
            Pk = categoryKey,
            Sk = categoryKey,
            Type = DynamoDbKeys.CategoryType,
            Gsi1Pk = DynamoDbKeys.CategoryListValue,
            Gsi1Sk = DynamoDbKeys.CategorySortKey(entity.Depth, entity.Name, entity.Id),
            Id = entity.Id.ToString(),
            Name = entity.Name,
            ParentCategoryId = entity.ParentCategoryId?.ToString(),
            Depth = entity.Depth
        };
    }

    public static CategoryEntity ToEntity(CategoryItem item)
    {
        return CategoryEntity.FromPersistence(
            Guid.Parse(item.Id),
            item.Name,
            string.IsNullOrEmpty(item.ParentCategoryId) ? null : Guid.Parse(item.ParentCategoryId),
            item.Depth);
    }

    public static Dictionary<string, AttributeValue> ToAttributeMap(CategoryItem item)
    {
        Dictionary<string, AttributeValue> map = new()
        {
            [DynamoDbKeys.Pk] = new AttributeValue(item.Pk),
            [DynamoDbKeys.Sk] = new AttributeValue(item.Sk),
            [DynamoDbKeys.Type] = new AttributeValue(item.Type),
            [DynamoDbKeys.Gsi1Pk] = new AttributeValue(item.Gsi1Pk),
            [DynamoDbKeys.Gsi1Sk] = new AttributeValue(item.Gsi1Sk),
            [IdAttribute] = new AttributeValue(item.Id),
            [NameAttribute] = new AttributeValue(item.Name),
            [DepthAttribute] = new AttributeValue { N = item.Depth.ToString(CultureInfo.InvariantCulture) }
        };

        if (!string.IsNullOrEmpty(item.ParentCategoryId))
            map[ParentCategoryIdAttribute] = new AttributeValue(item.ParentCategoryId);

        return map;
    }

    public static CategoryItem FromAttributeMap(Dictionary<string, AttributeValue> map)
    {
        return new CategoryItem
        {
            Pk = map[DynamoDbKeys.Pk].S,
            Sk = map[DynamoDbKeys.Sk].S,
            Type = map[DynamoDbKeys.Type].S,
            Gsi1Pk = map[DynamoDbKeys.Gsi1Pk].S,
            Gsi1Sk = map[DynamoDbKeys.Gsi1Sk].S,
            Id = map[IdAttribute].S,
            Name = map[NameAttribute].S,
            ParentCategoryId = map.TryGetValue(ParentCategoryIdAttribute, out AttributeValue? parentId) ? parentId.S : null,
            Depth = int.Parse(map[DepthAttribute].N, CultureInfo.InvariantCulture)
        };
    }
}
