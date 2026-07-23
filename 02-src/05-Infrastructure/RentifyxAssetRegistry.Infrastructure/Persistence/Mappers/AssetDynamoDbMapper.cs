using System.Globalization;
using Amazon.DynamoDBv2.Model;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Enums;
using RentifyxAssetRegistry.Domain.ValueObjects;
using RentifyxAssetRegistry.Infrastructure.Persistence.Items;

namespace RentifyxAssetRegistry.Infrastructure.Persistence.Mappers;

/// <summary>
/// Hand-written Domain &lt;-&gt; DynamoDB mapping for <see cref="AssetEntity"/>. Never relies on
/// attribute-driven property-name auto-mapping for the Domain-to-Item direction.
/// </summary>
public static class AssetDynamoDbMapper
{
    private const string IdAttribute = "Id";
    private const string OwnerIdAttribute = "OwnerId";
    private const string TitleAttribute = "Title";
    private const string DescriptionAttribute = "Description";
    private const string PriceAmountAttribute = "PriceAmount";
    private const string PriceCurrencyAttribute = "PriceCurrency";
    private const string CategoryIdAttribute = "CategoryId";
    private const string StatusAttribute = "Status";
    private const string IdempotencyKeyAttribute = "IdempotencyKey";
    private const string CreatedAtAttribute = "CreatedAt";
    private const string UpdatedAtAttribute = "UpdatedAt";

    public static AssetItem ToItem(AssetEntity entity)
    {
        string assetKey = DynamoDbKeys.AssetKey(entity.Id);
        string statusName = entity.Status.ToString();

        return new AssetItem
        {
            Pk = assetKey,
            Sk = assetKey,
            Type = DynamoDbKeys.AssetType,
            Gsi1Pk = DynamoDbKeys.OwnerKey(entity.OwnerId),
            Gsi1Sk = DynamoDbKeys.AssetSortKey(entity.CreatedAt, entity.Id),
            Gsi2Pk = DynamoDbKeys.CategoryPartitionKey(entity.CategoryId),
            Gsi2Sk = DynamoDbKeys.AssetSortKey(entity.CreatedAt, entity.Id),
            Gsi3Pk = DynamoDbKeys.IdempotencyKey(entity.IdempotencyKey),
            Gsi3Sk = assetKey,
            Gsi4Pk = DynamoDbKeys.StatusKey(statusName),
            Gsi4Sk = DynamoDbKeys.AssetSortKey(entity.CreatedAt, entity.Id),
            Id = entity.Id.ToString(),
            OwnerId = entity.OwnerId.ToString(),
            Title = entity.Title.Value,
            Description = entity.Description.Value,
            PriceAmount = entity.Price.Amount,
            PriceCurrency = entity.Price.Currency,
            CategoryId = entity.CategoryId.ToString(),
            Status = statusName,
            IdempotencyKey = entity.IdempotencyKey,
            CreatedAt = entity.CreatedAt.ToString("o", CultureInfo.InvariantCulture),
            UpdatedAt = entity.UpdatedAt?.ToString("o", CultureInfo.InvariantCulture)
        };
    }

    public static AssetEntity ToEntity(AssetItem item)
    {
        return AssetEntity.FromPersistence(
            Guid.Parse(item.Id),
            Guid.Parse(item.OwnerId),
            AssetTitle.Create(item.Title),
            AssetDescription.Create(item.Description),
            Money.Create(item.PriceAmount),
            Guid.Parse(item.CategoryId),
            Enum.Parse<AssetStatus>(item.Status),
            item.IdempotencyKey,
            DateTime.Parse(item.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            string.IsNullOrEmpty(item.UpdatedAt)
                ? null
                : DateTime.Parse(item.UpdatedAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
    }

    public static Dictionary<string, AttributeValue> ToAttributeMap(AssetItem item)
    {
        Dictionary<string, AttributeValue> map = new()
        {
            [DynamoDbKeys.Pk] = new AttributeValue(item.Pk),
            [DynamoDbKeys.Sk] = new AttributeValue(item.Sk),
            [DynamoDbKeys.Type] = new AttributeValue(item.Type),
            [DynamoDbKeys.Gsi1Pk] = new AttributeValue(item.Gsi1Pk),
            [DynamoDbKeys.Gsi1Sk] = new AttributeValue(item.Gsi1Sk),
            [DynamoDbKeys.Gsi2Pk] = new AttributeValue(item.Gsi2Pk),
            [DynamoDbKeys.Gsi2Sk] = new AttributeValue(item.Gsi2Sk),
            [DynamoDbKeys.Gsi3Pk] = new AttributeValue(item.Gsi3Pk),
            [DynamoDbKeys.Gsi3Sk] = new AttributeValue(item.Gsi3Sk),
            [DynamoDbKeys.Gsi4Pk] = new AttributeValue(item.Gsi4Pk),
            [DynamoDbKeys.Gsi4Sk] = new AttributeValue(item.Gsi4Sk),
            [IdAttribute] = new AttributeValue(item.Id),
            [OwnerIdAttribute] = new AttributeValue(item.OwnerId),
            [TitleAttribute] = new AttributeValue(item.Title),
            [DescriptionAttribute] = new AttributeValue(item.Description),
            [PriceAmountAttribute] = new AttributeValue { N = item.PriceAmount.ToString(CultureInfo.InvariantCulture) },
            [PriceCurrencyAttribute] = new AttributeValue(item.PriceCurrency),
            [CategoryIdAttribute] = new AttributeValue(item.CategoryId),
            [StatusAttribute] = new AttributeValue(item.Status),
            [IdempotencyKeyAttribute] = new AttributeValue(item.IdempotencyKey),
            [CreatedAtAttribute] = new AttributeValue(item.CreatedAt)
        };

        if (!string.IsNullOrEmpty(item.UpdatedAt))
            map[UpdatedAtAttribute] = new AttributeValue(item.UpdatedAt);

        return map;
    }

    public static AssetItem FromAttributeMap(Dictionary<string, AttributeValue> map)
    {
        return new AssetItem
        {
            Pk = map[DynamoDbKeys.Pk].S,
            Sk = map[DynamoDbKeys.Sk].S,
            Type = map[DynamoDbKeys.Type].S,
            Gsi1Pk = map[DynamoDbKeys.Gsi1Pk].S,
            Gsi1Sk = map[DynamoDbKeys.Gsi1Sk].S,
            Gsi2Pk = map[DynamoDbKeys.Gsi2Pk].S,
            Gsi2Sk = map[DynamoDbKeys.Gsi2Sk].S,
            Gsi3Pk = map[DynamoDbKeys.Gsi3Pk].S,
            Gsi3Sk = map[DynamoDbKeys.Gsi3Sk].S,
            Gsi4Pk = map[DynamoDbKeys.Gsi4Pk].S,
            Gsi4Sk = map[DynamoDbKeys.Gsi4Sk].S,
            Id = map[IdAttribute].S,
            OwnerId = map[OwnerIdAttribute].S,
            Title = map[TitleAttribute].S,
            Description = map[DescriptionAttribute].S,
            PriceAmount = decimal.Parse(map[PriceAmountAttribute].N, CultureInfo.InvariantCulture),
            PriceCurrency = map[PriceCurrencyAttribute].S,
            CategoryId = map[CategoryIdAttribute].S,
            Status = map[StatusAttribute].S,
            IdempotencyKey = map[IdempotencyKeyAttribute].S,
            CreatedAt = map[CreatedAtAttribute].S,
            UpdatedAt = map.TryGetValue(UpdatedAtAttribute, out AttributeValue? updatedAt) ? updatedAt.S : null
        };
    }
}
