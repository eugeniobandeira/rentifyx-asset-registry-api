using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.SubmitForModeration.Request;

namespace RentifyxAssetRegistry.Api.Endpoints.Assets;

internal sealed class SubmitForModeration : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/assets/{id:guid}/submit-for-moderation", HandleAsync)
           .WithName("SubmitForModeration")
           .WithDescription("Submits a Draft asset for moderation review.")
           .WithTags(Tags.ASSETS)
           .AllowAnonymous();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        SubmitForModerationBody body,
        IHandler<SubmitForModerationRequest, AssetModerationResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        SubmitForModerationRequest request = new(id, body.OwnerId);

        var result = await handler.HandleAsync(request, cancellationToken);

        return result.ToResult(httpContext);
    }

    private sealed record SubmitForModerationBody(Guid OwnerId);
}
