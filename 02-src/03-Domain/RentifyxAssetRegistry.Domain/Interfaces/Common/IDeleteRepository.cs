namespace RentifyxAssetRegistry.Domain.Interfaces.Common;

public interface IDeleteRepository<in T> where T : class
{
    Task DeleteAsync(T entity, CancellationToken ct = default);
}
