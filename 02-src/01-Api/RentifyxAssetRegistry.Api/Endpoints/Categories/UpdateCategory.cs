using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Categories;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Update.Request;

namespace RentifyxAssetRegistry.Api.Endpoints.Categories;

/// <summary>
/// Renames a category and/or moves it under a different parent (re-parenting). Admin-only
/// (ADR-AR-006); leaf categories only.
/// </summary>
internal sealed class UpdateCategory : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("/categories/{id:guid}", HandleAsync)
           .WithName("UpdateCategory")
           .WithSummary("Rename or re-parent a category")
           .WithDescription(
               "Renames or re-parents a leaf category (admin-only). Both newName and " +
               "newParentCategoryId are optional - supply either or both. isAdmin must be true " +
               "or the request is rejected as forbidden.")
           .WithTags(Tags.CATEGORIES)
           .AllowAnonymous()
           .AddOpenApiOperationTransformer(static (operation, _, _) =>
           {
               if (operation.RequestBody is OpenApiRequestBody { Content: not null } requestBody &&
                   requestBody.Content.TryGetValue("application/json", out var mediaType))
               {
                   mediaType.Example = new JsonObject
                   {
                       ["isAdmin"] = true,
                       ["newName"] = "Mini Excavators",
                       ["newParentCategoryId"] = "8c1f0b8a-2b3e-4c9a-9f0a-1a2b3c4d5e6f"
                   };
               }

               return Task.CompletedTask;
           });
    }

    /// <summary>Handles the update-category request and maps the result to 200 or a problem response.</summary>
    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateCategoryBody body,
        IHandler<UpdateCategoryRequest, CategoryResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        UpdateCategoryRequest request = new(id, body.IsAdmin, body.NewName, body.NewParentCategoryId);

        var result = await handler.HandleAsync(request, cancellationToken);

        return result.ToResult(httpContext);
    }

    /// <summary>Request body for <see cref="UpdateCategory"/>: an admin-privilege assertion and the optional new name/parent.</summary>
    private sealed record UpdateCategoryBody(
        bool IsAdmin,
        string? NewName,
        Guid? NewParentCategoryId
    );
}
