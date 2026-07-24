using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create.Request;
using ErrorOr;

namespace RentifyxAssetRegistry.Api.Endpoints.Assets;

internal sealed class CreateAsset : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/assets", HandleAsync)
           .WithName("CreateAsset")
           .WithDescription("Creates a new asset in Draft status.")
           .WithTags(Tags.ASSETS)
           .AllowAnonymous();
    }

    private static async Task<IResult> HandleAsync(
        CreateAssetRequest request,
        IHandler<CreateAssetRequest, CreateAssetResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ErrorOr<CreateAssetResponse> result = await handler.HandleAsync(request, cancellationToken);

        return result.Match(
            r => Results.Created($"/api/v1/assets/{r.AssetId}", r),
            errors => errors.ToProblem(httpContext));
    }
}
