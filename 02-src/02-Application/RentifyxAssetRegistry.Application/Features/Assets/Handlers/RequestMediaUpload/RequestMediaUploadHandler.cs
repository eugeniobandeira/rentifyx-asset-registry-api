using ErrorOr;
using FluentValidation;
using Microsoft.Extensions.Logging;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Extensions;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.RequestMediaUpload.Request;
using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;
using RentifyxAssetRegistry.Domain.Interfaces.Media;

namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.RequestMediaUpload;

public sealed class RequestMediaUploadHandler(
    IAssetRepository repository,
    IMediaStorageService mediaStorageService,
    IValidator<RequestMediaUploadRequest> validator,
    ILogger<RequestMediaUploadHandler> logger) : IHandler<RequestMediaUploadRequest, RequestMediaUploadResponse>
{
    public async Task<ErrorOr<RequestMediaUploadResponse>> HandleAsync(
        RequestMediaUploadRequest request,
        CancellationToken ct = default)
    {
        logger.LogInformation("Requesting media upload. Payload={@Payload}", request);

        List<Error>? errors = await validator.ValidateToErrorsAsync(request, ct);
        if (errors is not null)
            return errors;

        AssetEntity? asset = await repository.GetByIdAsync(request.AssetId, ct);
        if (asset is null)
            return Error.NotFound(AssetErrorCodes.NotFound, $"Asset {request.AssetId} not found.");

        if (asset.OwnerId != request.OwnerId)
            return Error.Forbidden(AssetErrorCodes.NotOwner, "OwnerId does not match the asset's owner.");

        PresignedUploadUrl presignedUrl = await mediaStorageService.GeneratePresignedUploadUrlAsync(
            request.OwnerId,
            request.AssetId,
            request.MimeType,
            request.SizeBytes,
            ct);

        logger.LogInformation("Presigned upload URL generated. AssetId={AssetId} S3Key={S3Key}", request.AssetId, presignedUrl.S3Key);

        return new RequestMediaUploadResponse(presignedUrl.Url, presignedUrl.S3Key);
    }
}
