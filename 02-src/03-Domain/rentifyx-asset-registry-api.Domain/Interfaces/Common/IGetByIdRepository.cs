namespace rentifyx_asset_registry_api.Domain.Interfaces.Common;

public interface IGetByIdRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
