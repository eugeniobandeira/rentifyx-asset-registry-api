using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Categories;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Create.Request;
using ErrorOr;

namespace RentifyxAssetRegistry.Api.Endpoints.Categories;

internal sealed class CreateCategory : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/categories", HandleAsync)
           .WithName("CreateCategory")
           .WithDescription("Creates a new root or child category (admin-only).")
           .WithTags(Tags.CATEGORIES)
           .AllowAnonymous();
    }

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
