namespace RentifyxAssetRegistry.Domain.Filters.Assets;

public sealed record AssetSearchFilter(
    int PageSize,
    string? NextPageToken = null,
    Guid? CategoryId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Keyword = null);
