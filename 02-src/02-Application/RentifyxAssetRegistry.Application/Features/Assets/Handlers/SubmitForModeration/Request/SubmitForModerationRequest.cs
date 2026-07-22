namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.SubmitForModeration.Request;

public sealed record SubmitForModerationRequest(
    Guid AssetId,
    Guid OwnerId
);
