using RentifyxAssetRegistry.Application.Common.Response;

namespace RentifyxAssetRegistry.Application.Common.Mapper;

public static class ApiListResponseMapper
{
    public static ApiListResponse<T> ToListResponse<T>(
        IReadOnlyCollection<T> data,
        int totalCount,
        int page,
        int pageSize)
        => new(data, totalCount, page, pageSize);
}
