namespace RentifyxAssetRegistry.Api.Messaging.Contracts;

// Real identity-api event name — not "UserDeleted".
public sealed record UserAccountDeletedPayload(Guid UserId, DateTimeOffset OccurredAt);
