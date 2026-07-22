using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Filters.Assets;
using RentifyxAssetRegistry.Domain.Interfaces.Common;

namespace RentifyxAssetRegistry.Domain.Interfaces.Asset;

public interface IAssetRepository :
    IGetByIdRepository<AssetEntity>,
    ISaveRepository<AssetEntity>,
    ISoftDeleteRepository,
    ISearchRepository<AssetEntity, AssetSearchFilter>
{
    Task<IReadOnlyList<AssetEntity>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default);

    Task<AssetEntity?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default);
}
