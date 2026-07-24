using RentifyxAssetRegistry.Domain.Enums;

namespace RentifyxAssetRegistry.Api.Messaging.Contracts;

/// <summary>
/// Mirrors rentifyx-ai-services' real AssetMediaModerated record (SchemaVersion 2, unenveloped,
/// includes Bucket/Key not previously known to this repo) — verified against that repo's shipped
/// code, not its own stale design doc. Verdict reuses this repo's existing ModerationVerdict enum
/// (same 3 names as ai-services' Verdict enum) so the default System.Text.Json enum-as-string
/// handling round-trips it, given ai-services serializes via JsonStringEnumConverter.
/// </summary>
public sealed record AssetMediaModeratedEvent(
    Guid AssetId,
    ModerationVerdict Verdict,
    IReadOnlyList<ModerationLabelDto> Labels,
    float TopConfidence,
    DateTimeOffset Timestamp,
    string Bucket,
    string Key,
    int SchemaVersion);
