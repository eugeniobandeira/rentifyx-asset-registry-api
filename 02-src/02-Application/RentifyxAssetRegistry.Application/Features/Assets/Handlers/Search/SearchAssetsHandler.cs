using ErrorOr;
using FluentValidation;
using Microsoft.Extensions.Logging;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Extensions;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Search.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Mapper;
using RentifyxAssetRegistry.Domain.Common;
using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Enums;
using RentifyxAssetRegistry.Domain.Filters.Assets;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;
using RentifyxAssetRegistry.Infrastructure.Persistence.Exceptions;

namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.Search;

public sealed class SearchAssetsHandler(
    IAssetRepository repository,
    IValidator<SearchAssetsRequest> validator,
    ILogger<SearchAssetsHandler> logger) : IHandler<SearchAssetsRequest, SearchAssetsResponse>
{
    public async Task<ErrorOr<SearchAssetsResponse>> HandleAsync(
        SearchAssetsRequest request,
        CancellationToken ct = default)
    {
        logger.LogDebug("Searching assets. Payload={@Payload}", request);

        List<Error>? errors = await validator.ValidateToErrorsAsync(request, ct);
        if (errors is not null)
            return errors;

        string? keyword = string.IsNullOrWhiteSpace(request.Keyword) ? null : request.Keyword;
        string? nextPageToken = string.IsNullOrWhiteSpace(request.NextPageToken) ? null : request.NextPageToken;

        AssetSearchFilter filter = new(
            request.PageSize,
            AssetStatus.Active,
            nextPageToken,
            request.CategoryId,
            request.MinPrice,
            request.MaxPrice,
            keyword);

        CursorPagedResult<AssetEntity> result;

        try
        {
            result = await repository.SearchAsync(filter, ct);
        }
        catch (InvalidPageTokenException ex)
        {
            logger.LogDebug(ex, "Rejected search request with an invalid page token.");
            return Error.Validation(AssetErrorCodes.InvalidPageToken, ex.Message);
        }

        logger.LogDebug("Found {Count} assets.", result.Items.Count);

        return AssetMapper.ToSearchAssetsResponse(result);
    }
}
