namespace RentifyxAssetRegistry.Domain.Interfaces.Common;

public interface ISoftDeleteRepository
{
    Task SoftDeleteAsync(Guid id, CancellationToken ct = default);
}
