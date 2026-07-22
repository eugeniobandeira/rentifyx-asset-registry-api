using RentifyxAssetRegistry.Domain.Enums;

namespace RentifyxAssetRegistry.Application.Features.Assets;

public sealed record AssetModerationResponse(
    Guid AssetId,
    AssetStatus Status
);
