using rentifyx_asset_registry_api.Domain.Interfaces;

namespace rentifyx_asset_registry_api.Infrastructure.Context;

public sealed class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task CommitAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
}
