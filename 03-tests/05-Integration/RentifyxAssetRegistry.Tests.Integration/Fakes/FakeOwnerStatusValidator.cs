using System.Collections.Concurrent;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;

namespace RentifyxAssetRegistry.Tests.Integration.Fakes;

public sealed class FakeOwnerStatusValidator : IOwnerStatusValidator
{
    private readonly ConcurrentDictionary<Guid, bool> _suspendedOwners = new();

    public void MarkSuspended(Guid ownerId) => _suspendedOwners[ownerId] = true;

    public Task<bool> IsOwnerActiveAsync(Guid ownerId, CancellationToken cancellationToken = default)
        => Task.FromResult(!_suspendedOwners.ContainsKey(ownerId));
}
