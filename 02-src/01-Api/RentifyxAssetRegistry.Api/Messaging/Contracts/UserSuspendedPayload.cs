namespace RentifyxAssetRegistry.Api.Messaging.Contracts;

/// <summary>
/// Inner Data payload when UserLifecycleEventEnvelope.EventType == "UserSuspended". Field names
/// verified against identity-api's real UserSuspended domain event record.
/// </summary>
public sealed record UserSuspendedPayload(Guid UserId, string Reason, DateTimeOffset OccurredAt);
