using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Categories;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Update.Request;

namespace RentifyxAssetRegistry.Api.Endpoints.Categories;

internal sealed class UpdateCategory : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("/categories/{id:guid}", HandleAsync)
           .WithName("UpdateCategory")
           .WithDescription("Renames or re-parents a leaf category (admin-only).")
           .WithTags(Tags.CATEGORIES)
           .AllowAnonymous();
    }

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

    private sealed record UpdateCategoryBody(
        bool IsAdmin,
        string? NewName,
        Guid? NewParentCategoryId
    );
}
