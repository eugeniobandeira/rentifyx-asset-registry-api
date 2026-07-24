using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.RequestMediaUpload.Request;

namespace RentifyxAssetRegistry.Api.Endpoints.Assets;

internal sealed class RequestMediaUpload : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/assets/{id:guid}/media/upload-request", HandleAsync)
           .WithName("RequestMediaUpload")
           .WithDescription("Validates a media upload and returns a presigned S3 upload URL.")
           .WithTags(Tags.ASSETS)
           .AllowAnonymous();
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        RequestMediaUploadBody body,
        IHandler<RequestMediaUploadRequest, RequestMediaUploadResponse> handler,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        RequestMediaUploadRequest request = new(id, body.OwnerId, body.MimeType, body.SizeBytes);

        var result = await handler.HandleAsync(request, cancellationToken);

        return result.ToResult(httpContext);
    }

    private sealed record RequestMediaUploadBody(
        Guid OwnerId,
        string MimeType,
        long SizeBytes
    );
}
