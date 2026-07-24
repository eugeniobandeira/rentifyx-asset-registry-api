using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ConfirmMediaUpload.Request;

namespace RentifyxAssetRegistry.Api.Endpoints.Assets;

/// <summary>
/// Confirms that a file previously presigned via <see cref="RequestMediaUpload"/> finished
/// uploading to S3, and attaches it to the asset.
/// </summary>
internal sealed class ConfirmMediaUpload : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/assets/{id:guid}/media/confirm", HandleAsync)
           .WithName("ConfirmMediaUpload")
           .WithSummary("Confirm a completed media upload")
           .WithDescription(
               "Confirms a completed media upload and attaches it to the asset. The s3Key must " +
               "match the key returned by a prior RequestMediaUpload call for the same asset.")
           .WithTags(Tags.ASSETS)
           .AllowAnonymous()
           .AddOpenApiOperationTransformer(static (operation, _, _) =>
           {
               if (operation.RequestBody is OpenApiRequestBody { Content: not null } requestBody &&
                   requestBody.Content.TryGetValue("application/json", out var mediaType))
               {
                   mediaType.Example = new JsonObject
                   {
                       ["ownerId"] = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                       ["s3Key"] = "assets/6f1a2b3c-4d5e-4f60-8a1b-2c3d4e5f6071/media/photo-1.jpg",
                       ["mimeType"] = "image/jpeg",
                       ["sizeBytes"] = 2_097_152
                   };
               }

               return Task.CompletedTask;
           });
    }

    /// <summary>Handles the confirm-media-upload request and maps the result to 200 or a problem response.</summary>
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

    /// <summary>Request body for <see cref="ConfirmMediaUpload"/>: the owner and the uploaded file's S3 key, MIME type, and size.</summary>
    private sealed record ConfirmMediaUploadBody(
        Guid OwnerId,
        string S3Key,
        string MimeType,
        long SizeBytes
    );
}
