using System.Text.Json.Nodes;
using ErrorOr;
using Microsoft.OpenApi;
using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Categories;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Create.Request;

namespace RentifyxAssetRegistry.Api.Endpoints.Categories;

/// <summary>
/// Creates a new root category (no parent) or child category (nested under an existing
/// category, up to the configured max depth). Admin-only (ADR-AR-006).
/// </summary>
internal sealed class CreateCategory : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/categories", HandleAsync)
           .WithName("CreateCategory")
           .WithSummary("Create a category")
           .WithDescription(
               "Creates a new root or child category (admin-only). Omit parentCategoryId for a " +
               "root category, or supply an existing category's id to nest under it - up to a " +
               "maximum depth of 3. isAdmin must be true or the request is rejected as forbidden.")
           .WithTags(Tags.CATEGORIES)
           .AllowAnonymous()
           .AddOpenApiOperationTransformer(static (operation, _, _) =>
           {
               if (operation.RequestBody is OpenApiRequestBody { Content: not null } requestBody &&
                   requestBody.Content.TryGetValue("application/json", out var mediaType))
               {
                   mediaType.Example = new JsonObject
                   {
                       ["name"] = "Excavators",
                       ["parentCategoryId"] = "8c1f0b8a-2b3e-4c9a-9f0a-1a2b3c4d5e6f",
                       ["isAdmin"] = true
                   };
               }

               return Task.CompletedTask;
           });
    }

    /// <summary>Handles the create-category request and maps the result to a 201 or a problem response.</summary>
    private static async Task<IResult> HandleAsync(
        CreateCategoryRequest request,
        IHandler<CreateCategoryRequest, CategoryResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ErrorOr<CategoryResponse> result = await handler.HandleAsync(request, cancellationToken);

        return result.Match(
            r => Results.Created($"/api/v1/categories/{r.Id}", r),
            errors => errors.ToProblem(httpContext));
    }
}
