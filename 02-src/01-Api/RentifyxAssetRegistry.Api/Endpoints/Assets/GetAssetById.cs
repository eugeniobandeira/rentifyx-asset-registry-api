using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.GetById.Request;

namespace RentifyxAssetRegistry.Api.Endpoints.Assets;

internal sealed class GetAssetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/assets/{id:guid}", HandleAsync)
           .WithName("GetAssetById")
           .WithDescription("Returns an asset by id.")
           .WithTags(Tags.ASSETS)
           .AllowAnonymous();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        IHandler<GetAssetByIdRequest, GetAssetByIdResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(new GetAssetByIdRequest(id), cancellationToken);

        return result.ToResult(httpContext);
    }
}
