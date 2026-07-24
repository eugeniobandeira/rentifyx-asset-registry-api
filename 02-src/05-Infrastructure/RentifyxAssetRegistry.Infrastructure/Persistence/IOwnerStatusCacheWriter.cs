namespace RentifyxAssetRegistry.Infrastructure.Persistence;

/// <summary>
/// Infrastructure-facing write path for the owner-status cache (F-12). Unlike
/// IOwnerStatusValidator (Domain-facing read, consumed by CreateAssetHandler), this is only
/// consumed by OwnerStatusConsumer (Api layer) — not a Domain concept, so it does not live under
/// Domain/Interfaces/. Public (not internal) because it is registered from the IoC project and
/// resolved from the Api project — both separate assemblies, no InternalsVisibleTo configured.
/// </summary>
public interface IOwnerStatusCacheWriter
{
    Task UpsertAsync(
        Guid ownerId,
        bool isActive,
        string reason,
        DateTimeOffset updatedAt,
        CancellationToken ct = default);
}
