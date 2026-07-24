using System.Collections.Concurrent;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Interfaces.Category;

namespace RentifyxAssetRegistry.Tests.Integration.Fakes;

public sealed class InMemoryCategoryRepository : ICategoryRepository
{
    private readonly ConcurrentDictionary<Guid, CategoryEntity> _categories = new();

    public Task<CategoryEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_categories.GetValueOrDefault(id));

    public Task<IReadOnlyList<CategoryEntity>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<CategoryEntity>>(_categories.Values.ToList());

    public Task SaveAsync(CategoryEntity entity, CancellationToken ct = default)
    {
        _categories[entity.Id] = entity;
        return Task.CompletedTask;
    }
}
