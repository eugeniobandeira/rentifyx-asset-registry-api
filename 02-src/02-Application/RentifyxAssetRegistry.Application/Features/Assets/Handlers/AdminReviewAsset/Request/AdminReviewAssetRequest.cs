namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.AdminReviewAsset.Request;

public sealed record AdminReviewAssetRequest(
    Guid AssetId,
    bool Approve,
    bool IsAdmin,
    string? Reason = null
);
