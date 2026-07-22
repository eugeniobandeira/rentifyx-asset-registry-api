using RentifyxAssetRegistry.Domain.Enums;

namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.ApplyModerationVerdict.Request;

public sealed record ApplyModerationVerdictRequest(
    Guid AssetId,
    ModerationVerdict Verdict
);
