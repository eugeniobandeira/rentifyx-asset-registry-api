namespace RentifyxAssetRegistry.Domain.Interfaces.Common;

public interface IUpdateRepository<in T> where T : class
{
    Task UpdateAsync(T entity, CancellationToken ct = default);
}
