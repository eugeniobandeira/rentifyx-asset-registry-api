using RentifyxAssetRegistry.Domain.Common;

namespace RentifyxAssetRegistry.Domain.Events.Asset;

public sealed record AssetPublished(
    Guid AssetId,
    DateTime OccurredAt) : IDomainEvent;
