using System.Globalization;
using RentifyxAssetRegistry.Infrastructure.Persistence.Items;

namespace RentifyxAssetRegistry.Infrastructure.Persistence.Mappers;

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
