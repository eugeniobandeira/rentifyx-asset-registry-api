using System.Globalization;
using RentifyxAssetRegistry.Infrastructure.Persistence.Items;

namespace RentifyxAssetRegistry.Infrastructure.Persistence.Mappers;

/// <summary>
/// Builds the OwnerStatus item shape. Unlike AssetDynamoDbMapper/OutboxDynamoDbMapper, no
/// AttributeValue-map round-trip is needed here — DynamoDbOwnerStatusValidator reads/writes via
/// IDynamoDBContext (object mapper), matching DynamoDbCategoryRepository's simpler precedent since
/// this item has no GSI and no Domain entity of its own.
/// </summary>
public static class OwnerStatusDynamoDbMapper
{
    public static OwnerStatusItem ToItem(Guid ownerId, bool isActive, string reason, DateTimeOffset updatedAt)
    {
        string key = DynamoDbKeys.OwnerStatusKey(ownerId);

        return new OwnerStatusItem
        {
            Pk = key,
            Sk = DynamoDbKeys.MetadataSortKey,
            Type = DynamoDbKeys.OwnerStatusType,
            OwnerId = ownerId.ToString(),
            IsActive = isActive,
            Reason = reason,
            UpdatedAt = updatedAt.ToString("o", CultureInfo.InvariantCulture)
        };
    }
}
