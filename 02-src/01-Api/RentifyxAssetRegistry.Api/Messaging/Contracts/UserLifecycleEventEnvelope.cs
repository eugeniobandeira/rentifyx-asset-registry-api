using System.Text.Json;

namespace RentifyxAssetRegistry.Api.Messaging.Contracts;

/// <summary>
/// Envelope wrapping every message on identity-api's user-lifecycle-events topic (UserSuspended,
/// UserAccountDeleted). Verified against identity-api's real UserLifecycleEventEnvelope record —
/// see .specs/features/e04-f12-cross-service-integration/spec.md. No SchemaVersion field exists on
/// this topic (confirmed, not assumed).
/// </summary>
public sealed record UserLifecycleEventEnvelope(
    string EventType,
    Guid AggregateId,
    DateTimeOffset OccurredAt,
    JsonElement Data);
