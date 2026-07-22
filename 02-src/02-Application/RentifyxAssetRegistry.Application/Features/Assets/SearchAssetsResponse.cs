using RentifyxAssetRegistry.Domain.Enums;

namespace RentifyxAssetRegistry.Application.Features.Assets;

public sealed record SearchAssetsResponse(
    IReadOnlyCollection<AssetSummaryResponse> Items,
    string? NextPageToken
);

public sealed record AssetSummaryResponse(
    Guid Id,
    string Title,
    decimal Price,
    Guid CategoryId,
    AssetStatus Status
);
