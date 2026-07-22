using RentifyxAssetRegistry.Domain.Enums;

namespace RentifyxAssetRegistry.Application.Features.Assets;

public sealed record CreateAssetResponse(
    Guid AssetId,
    AssetStatus Status,
    DateTime CreatedAt
);
