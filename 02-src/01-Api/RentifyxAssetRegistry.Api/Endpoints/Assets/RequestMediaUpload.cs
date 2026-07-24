using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.RequestMediaUpload.Request;

namespace RentifyxAssetRegistry.Api.Endpoints.Assets;

/// <summary>
/// Validates a proposed media upload (MIME type and size) and, if it passes, returns a presigned
/// S3 URL the caller uploads the file to directly. Validation happens before URL generation
/// (ADR-AR-005) so rejected files never reach S3.
/// </summary>
internal sealed class RequestMediaUpload : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/assets/{id:guid}/media/upload-request", HandleAsync)
           .WithName("RequestMediaUpload")
           .WithSummary("Request a presigned media upload URL")
           .WithDescription(
               "Validates a media upload's MIME type and size and returns a presigned S3 upload " +
               "URL. Allowed MIME types: image/jpeg, image/png, image/webp, video/mp4. Call " +
               "ConfirmMediaUpload once the file has been uploaded to the returned URL.")
           .WithTags(Tags.ASSETS)
           .AllowAnonymous()
           .Produces<RequestMediaUploadResponse>(StatusCodes.Status200OK)
           .AddOpenApiOperationTransformer(static (operation, _, _) =>
           {
               if (operation.RequestBody is OpenApiRequestBody { Content: not null } requestBody &&
                   requestBody.Content.TryGetValue("application/json", out var requestMediaType))
               {
                   requestMediaType.Example = new JsonObject
                   {
                       ["ownerId"] = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                       ["mimeType"] = "image/jpeg",
                       ["sizeBytes"] = 2_097_152
                   };
               }

               if (operation.Responses is not null &&
                   operation.Responses.TryGetValue("200", out var response) &&
                   response is OpenApiResponse { Content: not null } concreteResponse &&
                   concreteResponse.Content.TryGetValue("application/json", out var responseMediaType))
               {
                   responseMediaType.Example = new JsonObject
                   {
                       ["uploadUrl"] = "https://rentifyx-asset-media.s3.amazonaws.com/assets/" +
                           "6f1a2b3c-4d5e-4f60-8a1b-2c3d4e5f6071/media/photo-1.jpg?X-Amz-Signature=...",
                       ["s3Key"] = "assets/6f1a2b3c-4d5e-4f60-8a1b-2c3d4e5f6071/media/photo-1.jpg"
                   };
               }

               return Task.CompletedTask;
           });
    }

    /// <summary>Handles the media-upload-request and maps the result to 200 or a problem response.</summary>
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

    /// <summary>Request body for <see cref="RequestMediaUpload"/>: the owner and the file's declared MIME type and size.</summary>
    private sealed record RequestMediaUploadBody(
        Guid OwnerId,
        string MimeType,
        long SizeBytes
    );
}
