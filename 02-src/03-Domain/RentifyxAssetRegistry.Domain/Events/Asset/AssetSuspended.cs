using RentifyxAssetRegistry.Domain.Common;

namespace RentifyxAssetRegistry.Domain.Events.Asset;

public sealed record AssetSuspended(
    Guid AssetId,
    string Reason,
    Guid SuspendedBy,
    DateTime OccurredAt) : IDomainEvent;
