namespace RentifyxAssetRegistry.Domain.Interfaces.Asset;

public interface IOwnerStatusValidator
{
    Task<bool> IsOwnerActiveAsync(Guid ownerId, CancellationToken cancellationToken = default);
}
