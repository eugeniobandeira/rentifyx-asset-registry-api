using System.Text.Json;

namespace RentifyxAssetRegistry.Api.Messaging.Contracts;

public sealed record UserLifecycleEventEnvelope(
    string EventType,
    Guid AggregateId,
    DateTimeOffset OccurredAt,
    JsonElement Data);
