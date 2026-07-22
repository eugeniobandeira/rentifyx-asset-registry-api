using Microsoft.EntityFrameworkCore;
using RentifyxAssetRegistry.Domain.Common;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Filters.Examples;
using RentifyxAssetRegistry.Domain.Interfaces.Examples;
using RentifyxAssetRegistry.Infrastructure.Context;

namespace RentifyxAssetRegistry.Infrastructure.Repositories;

public sealed class ExampleRepository(AppDbContext dbContext) : IExampleRepository
{
    public async Task AddAsync(ExampleEntity entity, CancellationToken ct = default)
        => await dbContext.Examples.AddAsync(entity, ct);

    public async Task<ExampleEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await dbContext.Examples.FindAsync([id], ct);

    public async Task<PagedResult<ExampleEntity>> GetAllAsync(
        ExampleFilter filter,
        CancellationToken ct = default)
    {
        IQueryable<ExampleEntity> query = dbContext.Examples.AsNoTracking();

        if (filter.Name is not null)
            query = query.Where(e => e.Name.Contains(filter.Name));

        if (filter.IsActive is not null)
            query = query.Where(e => e.IsActive == filter.IsActive);

        int total = await query.CountAsync(ct);

        List<ExampleEntity> items = await query
            .OrderBy(e => e.Name)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return new PagedResult<ExampleEntity>(items, total);
    }

    public Task UpdateAsync(ExampleEntity entity, CancellationToken ct = default)
    {
        dbContext.Examples.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ExampleEntity entity, CancellationToken ct = default)
    {
        dbContext.Examples.Remove(entity);
        return Task.CompletedTask;
    }
}
