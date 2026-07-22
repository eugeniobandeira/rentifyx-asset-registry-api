using RentifyxAssetRegistry.Domain.Common;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Filters.Assets;

namespace RentifyxAssetRegistry.Domain.Interfaces.Asset;

public interface IAssetRepository
{
    Task<AssetEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssetEntity>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);

    Task SaveAsync(AssetEntity asset, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<AssetEntity>> SearchAsync(AssetSearchFilter filter, CancellationToken cancellationToken = default);
}
