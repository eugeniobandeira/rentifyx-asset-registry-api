namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.Search.Request;

public sealed record SearchAssetsRequest(
    int PageSize,
    string? NextPageToken = null,
    Guid? CategoryId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Keyword = null
);
