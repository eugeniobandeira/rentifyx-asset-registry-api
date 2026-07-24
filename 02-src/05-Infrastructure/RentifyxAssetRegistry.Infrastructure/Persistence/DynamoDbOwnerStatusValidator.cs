using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;
using RentifyxAssetRegistry.Infrastructure.Configuration;
using RentifyxAssetRegistry.Infrastructure.Persistence.Items;
using RentifyxAssetRegistry.Infrastructure.Persistence.Mappers;

namespace RentifyxAssetRegistry.Infrastructure.Persistence;

/// <summary>
/// Fail-closed by design: an owner with no cache entry is treated as NOT active.
/// </summary>
public sealed class DynamoDbOwnerStatusValidator(
    IDynamoDBContext context,
    DynamoDbOptions options) : IOwnerStatusValidator, IOwnerStatusCacheWriter
{
    public async Task<bool> IsOwnerActiveAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        string key = DynamoDbKeys.OwnerStatusKey(ownerId);

        OwnerStatusItem? item = await context.LoadAsync<OwnerStatusItem>(
            key,
            DynamoDbKeys.MetadataSortKey,
            new LoadConfig { OverrideTableName = options.TableName },
            cancellationToken);

        return item?.IsActive ?? false;
    }

    public async Task UpsertAsync(
        Guid ownerId,
        bool isActive,
        string reason,
        DateTimeOffset updatedAt,
        CancellationToken ct = default)
    {
        OwnerStatusItem item = OwnerStatusDynamoDbMapper.ToItem(ownerId, isActive, reason, updatedAt);

        await context.SaveAsync(item, new SaveConfig { OverrideTableName = options.TableName }, ct);
    }
}
