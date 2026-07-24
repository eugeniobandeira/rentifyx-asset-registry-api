namespace RentifyxAssetRegistry.Api.Messaging.Contracts;

public sealed record UserSuspendedPayload(Guid UserId, string Reason, DateTimeOffset OccurredAt);
