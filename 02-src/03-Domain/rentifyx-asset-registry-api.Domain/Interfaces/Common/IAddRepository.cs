namespace rentifyx_asset_registry_api.Domain.Interfaces.Common;

public interface IAddRepository<in T> where T : class
{
    Task AddAsync(T entity, CancellationToken ct = default);
}
