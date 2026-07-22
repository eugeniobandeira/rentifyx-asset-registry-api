using RentifyxAssetRegistry.Domain.Common;

namespace RentifyxAssetRegistry.Domain.Interfaces.Common;

public interface IGetAllRepository<T>
    where T : class
{
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
}

public interface IGetAllRepository<T, TFilter>
    where T : class
    where TFilter : class
{
    Task<PagedResult<T>> GetAllAsync(TFilter filter, CancellationToken ct = default);
}
