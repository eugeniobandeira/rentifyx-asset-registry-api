using RentifyxAssetRegistry.Domain.Common;

namespace RentifyxAssetRegistry.Domain.Events.Asset;

public sealed record AssetMediaUploaded(
    Guid AssetId,
    string S3Key,
    DateTime OccurredAt) : IDomainEvent;
