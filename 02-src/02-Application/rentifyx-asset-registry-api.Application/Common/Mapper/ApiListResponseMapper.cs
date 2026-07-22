using rentifyx_asset_registry_api.Application.Common.Response;

namespace rentifyx_asset_registry_api.Application.Common.Mapper;

public static class ApiListResponseMapper
{
    public static ApiListResponse<T> ToListResponse<T>(
        IReadOnlyCollection<T> data,
        int totalCount,
        int page,
        int pageSize)
        => new(data, totalCount, page, pageSize);
}
