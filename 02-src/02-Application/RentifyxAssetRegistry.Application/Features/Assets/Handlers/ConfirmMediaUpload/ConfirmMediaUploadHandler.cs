using ErrorOr;
using FluentValidation;
using Microsoft.Extensions.Logging;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Extensions;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ConfirmMediaUpload.Request;
using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Enums;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;
using RentifyxAssetRegistry.Domain.ValueObjects;

namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.ConfirmMediaUpload;

public sealed class ConfirmMediaUploadHandler(
    IAssetRepository repository,
    IValidator<ConfirmMediaUploadRequest> validator,
    ILogger<ConfirmMediaUploadHandler> logger) : IHandler<ConfirmMediaUploadRequest, ConfirmMediaUploadResponse>
{
    public async Task<ErrorOr<ConfirmMediaUploadResponse>> HandleAsync(
        ConfirmMediaUploadRequest request,
        CancellationToken ct = default)
    {
        logger.LogInformation("Confirming media upload. Payload={@Payload}", request);

        List<Error>? errors = await validator.ValidateToErrorsAsync(request, ct);
        if (errors is not null)
            return errors;

        AssetEntity? asset = await repository.GetByIdAsync(request.AssetId, ct);
        if (asset is null)
            return Error.NotFound(AssetErrorCodes.NotFound, $"Asset {request.AssetId} not found.");

        if (asset.OwnerId != request.OwnerId)
            return Error.Forbidden(AssetErrorCodes.NotOwner, "OwnerId does not match the asset's owner.");

        Media media = Media.Create(request.S3Key, request.MimeType, request.SizeBytes, MediaUploadStatus.Uploaded);

        asset.AttachMedia(media);

        await repository.SaveAsync(asset, ct);

        logger.LogInformation("Media upload confirmed. AssetId={AssetId} S3Key={S3Key}", asset.Id, request.S3Key);

        return new ConfirmMediaUploadResponse(asset.Id, request.S3Key);
    }
}
