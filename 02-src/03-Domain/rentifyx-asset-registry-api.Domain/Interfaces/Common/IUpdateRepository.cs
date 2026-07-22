namespace rentifyx_asset_registry_api.Domain.Interfaces.Common;

public interface IUpdateRepository<in T> where T : class
{
    Task UpdateAsync(T entity, CancellationToken ct = default);
}
