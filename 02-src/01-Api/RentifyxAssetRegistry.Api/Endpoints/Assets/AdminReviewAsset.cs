using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.AdminReviewAsset.Request;

namespace RentifyxAssetRegistry.Api.Endpoints.Assets;

internal sealed class AdminReviewAsset : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/assets/{id:guid}/admin-review", HandleAsync)
           .WithName("AdminReviewAsset")
           .WithDescription("Admin override for a PendingModeration asset: approve or reject.")
           .WithTags(Tags.ASSETS)
           .AllowAnonymous();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        AdminReviewAssetBody body,
        IHandler<AdminReviewAssetRequest, AssetModerationResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        AdminReviewAssetRequest request = new(id, body.Approve, body.IsAdmin, body.Reason);

        var result = await handler.HandleAsync(request, cancellationToken);

        return result.ToResult(httpContext);
    }

    private sealed record AdminReviewAssetBody(
        bool Approve,
        bool IsAdmin,
        string? Reason = null
    );
}
