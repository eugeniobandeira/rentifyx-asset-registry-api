using System.Collections.Concurrent;
using RentifyxAssetRegistry.Domain.Common;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Filters.Assets;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;

namespace RentifyxAssetRegistry.Tests.Integration.Fakes;

public sealed class InMemoryAssetRepository : IAssetRepository
{
    private readonly ConcurrentDictionary<Guid, AssetEntity> _assets = new();

    public Task<AssetEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_assets.GetValueOrDefault(id));

    public Task SaveAsync(AssetEntity entity, CancellationToken ct = default)
    {
        _assets[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task SoftDeleteAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;

    public Task<CursorPagedResult<AssetEntity>> SearchAsync(AssetSearchFilter filter, CancellationToken ct = default)
    {
        IEnumerable<AssetEntity> query = _assets.Values.Where(a => a.Status == filter.Status);

        if (filter.CategoryId is not null)
            query = query.Where(a => a.CategoryId == filter.CategoryId);

        if (filter.MinPrice is not null)
            query = query.Where(a => a.Price.Amount >= filter.MinPrice);

        if (filter.MaxPrice is not null)
            query = query.Where(a => a.Price.Amount <= filter.MaxPrice);

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
            query = query.Where(a => a.Title.Value.Contains(filter.Keyword, StringComparison.OrdinalIgnoreCase));

        List<AssetEntity> items = query.Take(filter.PageSize).ToList();

        return Task.FromResult(new CursorPagedResult<AssetEntity>(items, null));
    }

    public Task<IReadOnlyList<AssetEntity>> GetByOwnerAsync(Guid ownerId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<AssetEntity>>(_assets.Values.Where(a => a.OwnerId == ownerId).ToList());

    public Task<AssetEntity?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct = default)
        => Task.FromResult(_assets.Values.FirstOrDefault(a => a.IdempotencyKey == idempotencyKey));
}
