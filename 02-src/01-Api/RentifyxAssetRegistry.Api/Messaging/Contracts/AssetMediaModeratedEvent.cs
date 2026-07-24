using RentifyxAssetRegistry.Domain.Enums;

namespace RentifyxAssetRegistry.Api.Messaging.Contracts;

public sealed record AssetMediaModeratedEvent(
    Guid AssetId,
    ModerationVerdict Verdict,
    IReadOnlyList<ModerationLabelDto> Labels,
    float TopConfidence,
    DateTimeOffset Timestamp,
    string Bucket,
    string Key,
    int SchemaVersion);
