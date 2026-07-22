using RentifyxAssetRegistry.Domain.Common;

namespace RentifyxAssetRegistry.Domain.Events.Asset;

public sealed record AssetCreated(
    Guid AssetId,
    Guid OwnerId,
    Guid CategoryId,
    DateTime OccurredAt) : IDomainEvent;
