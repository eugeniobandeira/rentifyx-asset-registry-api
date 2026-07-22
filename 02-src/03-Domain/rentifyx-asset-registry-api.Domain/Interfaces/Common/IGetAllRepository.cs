using rentifyx_asset_registry_api.Domain.Common;

namespace rentifyx_asset_registry_api.Domain.Interfaces.Common;

public interface IGetAllRepository<T, TFilter>
    where T : class
    where TFilter : class
{
    Task<PagedResult<T>> GetAllAsync(TFilter filter, CancellationToken ct = default);
}
