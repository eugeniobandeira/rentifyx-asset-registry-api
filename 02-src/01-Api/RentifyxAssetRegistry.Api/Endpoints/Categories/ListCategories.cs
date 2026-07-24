using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Categories;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.ListCategories.Request;

namespace RentifyxAssetRegistry.Api.Endpoints.Categories;

internal sealed class ListCategories : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/categories", HandleAsync)
           .WithName("ListCategories")
           .WithDescription("Lists all categories.")
           .WithTags(Tags.CATEGORIES)
           .AllowAnonymous();
    }

    private static async Task<IResult> HandleAsync(
        IHandler<ListCategoriesRequest, IReadOnlyList<CategoryResponse>> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(new ListCategoriesRequest(), cancellationToken);

        return result.ToResult(httpContext);
    }
}
