using RentifyxAssetRegistry.Domain.Interfaces;

namespace RentifyxAssetRegistry.Infrastructure.Context;

public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task CommitAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
}
