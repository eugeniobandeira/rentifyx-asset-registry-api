namespace RentifyxAssetRegistry.Api.Messaging.Contracts;

/// <summary>
/// Inner Data payload when UserLifecycleEventEnvelope.EventType == "UserAccountDeleted". Real event
/// name confirmed against identity-api's code — NOT "UserDeleted" (an earlier, unverified assumption
/// in this repo's own docs before F-12's research pass).
/// </summary>
public sealed record UserAccountDeletedPayload(Guid UserId, DateTimeOffset OccurredAt);
