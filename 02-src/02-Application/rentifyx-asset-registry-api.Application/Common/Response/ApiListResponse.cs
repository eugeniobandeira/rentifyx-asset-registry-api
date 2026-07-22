namespace rentifyx_asset_registry_api.Application.Common.Response;

public sealed record ApiListResponse<T>(
    IReadOnlyCollection<T> Data,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;
}
