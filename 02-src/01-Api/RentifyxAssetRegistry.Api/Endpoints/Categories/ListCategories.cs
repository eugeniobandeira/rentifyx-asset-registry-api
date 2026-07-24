using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Categories;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.ListCategories.Request;

namespace RentifyxAssetRegistry.Api.Endpoints.Categories;

/// <summary>Lists every category in the catalog, flat (not nested), including their parent/depth metadata.</summary>
internal sealed class ListCategories : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/categories", HandleAsync)
           .WithName("ListCategories")
           .WithSummary("List all categories")
           .WithDescription(
               "Lists all categories. The result is a flat list; use parentCategoryId and depth " +
               "on each item to reconstruct the hierarchy client-side.")
           .WithTags(Tags.CATEGORIES)
           .AllowAnonymous()
           .Produces<IReadOnlyList<CategoryResponse>>(StatusCodes.Status200OK)
           .AddOpenApiOperationTransformer(static (operation, _, _) =>
           {
               if (operation.Responses is not null &&
                   operation.Responses.TryGetValue("200", out var response) &&
                   response is OpenApiResponse { Content: not null } concreteResponse &&
                   concreteResponse.Content.TryGetValue("application/json", out var mediaType))
               {
                   mediaType.Example = new JsonArray
                   {
                       new JsonObject
                       {
                           ["id"] = "8c1f0b8a-2b3e-4c9a-9f0a-1a2b3c4d5e6f",
                           ["name"] = "Heavy Machinery",
                           ["parentCategoryId"] = null,
                           ["depth"] = 0
                       },
                       new JsonObject
                       {
                           ["id"] = "1d2e3f4a-5b6c-4d7e-8f90-1a2b3c4d5e6f",
                           ["name"] = "Excavators",
                           ["parentCategoryId"] = "8c1f0b8a-2b3e-4c9a-9f0a-1a2b3c4d5e6f",
                           ["depth"] = 1
                       }
                   };
               }

               return Task.CompletedTask;
           });
    }

    /// <summary>Handles the list-categories request and maps the result to 200 or a problem response.</summary>
    private static async Task<IResult> HandleAsync(
        IHandler<ListCategoriesRequest, IReadOnlyList<CategoryResponse>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(new ListCategoriesRequest(), cancellationToken);

        return result.ToResult(httpContext);
    }
}
