using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Search.Request;

namespace RentifyxAssetRegistry.Api.Endpoints.Assets;

internal sealed class SearchAssets : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/assets", HandleAsync)
           .WithName("SearchAssets")
           .WithDescription("Searches published assets by category, price range and keyword, cursor-paginated.")
           .WithTags(Tags.ASSETS)
           .AllowAnonymous();
    }

    private static async Task<IResult> HandleAsync(
        int pageSize,
        string? nextPageToken,
        Guid? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        string? keyword,
        IHandler<SearchAssetsRequest, SearchAssetsResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        SearchAssetsRequest request = new(pageSize, nextPageToken, categoryId, minPrice, maxPrice, keyword);

        var result = await handler.HandleAsync(request, cancellationToken);

        return result.ToResult(httpContext);
    }
}
