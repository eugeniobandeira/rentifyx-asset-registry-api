namespace RentifyxAssetRegistry.Domain.Interfaces.Common;

public interface ISaveRepository<in T> where T : class
{
    Task SaveAsync(T entity, CancellationToken ct = default);
}
