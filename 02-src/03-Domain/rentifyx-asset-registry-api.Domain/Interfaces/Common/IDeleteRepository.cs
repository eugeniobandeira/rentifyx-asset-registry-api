namespace rentifyx_asset_registry_api.Domain.Interfaces.Common;

public interface IDeleteRepository<in T> where T : class
{
    Task DeleteAsync(T entity, CancellationToken ct = default);
}
