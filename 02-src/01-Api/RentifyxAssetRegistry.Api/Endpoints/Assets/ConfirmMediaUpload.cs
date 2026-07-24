using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ConfirmMediaUpload.Request;

namespace RentifyxAssetRegistry.Api.Endpoints.Assets;

internal sealed class ConfirmMediaUpload : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/assets/{id:guid}/media/confirm", HandleAsync)
           .WithName("ConfirmMediaUpload")
           .WithDescription("Confirms a completed media upload and attaches it to the asset.")
           .WithTags(Tags.ASSETS)
           .AllowAnonymous();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        ConfirmMediaUploadBody body,
        IHandler<ConfirmMediaUploadRequest, ConfirmMediaUploadResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        ConfirmMediaUploadRequest request = new(id, body.OwnerId, body.S3Key, body.MimeType, body.SizeBytes);

        var result = await handler.HandleAsync(request, cancellationToken);

        return result.ToResult(httpContext);
    }

    private sealed record ConfirmMediaUploadBody(
        Guid OwnerId,
        string S3Key,
        string MimeType,
        long SizeBytes
    );
}
