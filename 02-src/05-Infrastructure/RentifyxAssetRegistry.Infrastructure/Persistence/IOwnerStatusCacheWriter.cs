namespace RentifyxAssetRegistry.Infrastructure.Persistence;

// Public, not internal: registered from the IoC project and resolved from the Api project — both
// separate assemblies, no InternalsVisibleTo configured between them.
public interface IOwnerStatusCacheWriter
{
    Task UpsertAsync(
        Guid ownerId,
        bool isActive,
        string reason,
        DateTimeOffset updatedAt,
        CancellationToken ct = default);
}
