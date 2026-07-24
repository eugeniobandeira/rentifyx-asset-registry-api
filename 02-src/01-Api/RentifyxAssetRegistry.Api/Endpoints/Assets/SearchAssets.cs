using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Search.Request;

namespace RentifyxAssetRegistry.Api.Endpoints.Assets;

/// <summary>
/// Searches published assets, cursor-paginated and optionally filtered by category, price range,
/// and free-text keyword.
/// </summary>
internal sealed class SearchAssets : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/assets", HandleAsync)
           .WithName("SearchAssets")
           .WithSummary("Search assets")
           .WithDescription(
               "Searches published assets by category, price range and keyword, cursor-paginated. " +
               "pageSize must be between 1 and 30. Pass the previous response's nextPageToken to " +
               "fetch the next page.")
           .WithTags(Tags.ASSETS)
           .AllowAnonymous()
           .Produces<SearchAssetsResponse>(StatusCodes.Status200OK)
           .AddOpenApiOperationTransformer(static (operation, _, _) =>
           {
               if (operation.Responses is not null &&
                   operation.Responses.TryGetValue("200", out var response) &&
                   response is OpenApiResponse { Content: not null } concreteResponse &&
                   concreteResponse.Content.TryGetValue("application/json", out var mediaType))
               {
                   mediaType.Example = new JsonObject
                   {
                       ["items"] = new JsonArray
                       {
                           new JsonObject
                           {
                               ["id"] = "6f1a2b3c-4d5e-4f60-8a1b-2c3d4e5f6071",
                               ["title"] = "Compact Excavator CAT 305E2",
                               ["price"] = 285.00m,
                               ["categoryId"] = "8c1f0b8a-2b3e-4c9a-9f0a-1a2b3c4d5e6f",
                               ["status"] = 2
                           },
                           new JsonObject
                           {
                               ["id"] = "9b8c7d6e-5f40-4a3b-9c2d-1e0f9a8b7c6d",
                               ["title"] = "Tower Crane Liebherr 132EC-H6",
                               ["price"] = 1200.00m,
                               ["categoryId"] = "8c1f0b8a-2b3e-4c9a-9f0a-1a2b3c4d5e6f",
                               ["status"] = 2
                           }
                       },
                       ["nextPageToken"] = null
                   };
               }

               return Task.CompletedTask;
           });
    }

    /// <summary>Handles the search-assets request and maps the result to 200 or a problem response.</summary>
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
