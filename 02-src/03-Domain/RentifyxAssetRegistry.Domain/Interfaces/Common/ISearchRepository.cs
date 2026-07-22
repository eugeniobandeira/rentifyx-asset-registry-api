using RentifyxAssetRegistry.Domain.Common;

namespace RentifyxAssetRegistry.Domain.Interfaces.Common;

public interface ISearchRepository<T, in TFilter>
    where T : class
    where TFilter : class
{
    Task<CursorPagedResult<T>> SearchAsync(TFilter filter, CancellationToken ct = default);
}
