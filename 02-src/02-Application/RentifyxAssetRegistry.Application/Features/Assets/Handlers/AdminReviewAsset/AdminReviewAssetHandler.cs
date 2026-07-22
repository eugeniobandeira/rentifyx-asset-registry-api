using ErrorOr;
using FluentValidation;
using Microsoft.Extensions.Logging;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Extensions;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.AdminReviewAsset.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Mapper;
using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Enums;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;

namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.AdminReviewAsset;

public sealed class AdminReviewAssetHandler(
    IAssetRepository repository,
    IValidator<AdminReviewAssetRequest> validator,
    ILogger<AdminReviewAssetHandler> logger) : IHandler<AdminReviewAssetRequest, AssetModerationResponse>
{
    public async Task<ErrorOr<AssetModerationResponse>> HandleAsync(
        AdminReviewAssetRequest request,
        CancellationToken ct = default)
    {
        logger.LogInformation("Admin reviewing asset. Payload={@Payload}", request);

        List<Error>? errors = await validator.ValidateToErrorsAsync(request, ct);
        if (errors is not null)
            return errors;

        if (!request.IsAdmin)
            return Error.Forbidden(AssetErrorCodes.NotAdmin, "Only admins can review assets.");

        AssetEntity? asset = await repository.GetByIdAsync(request.AssetId, ct);
        if (asset is null)
            return Error.NotFound(AssetErrorCodes.NotFound, $"Asset {request.AssetId} not found.");

        if (asset.Status != AssetStatus.PendingModeration)
            return Error.Validation(AssetErrorCodes.InvalidStatus, $"Asset must be in 'PendingModeration' status. Current status is '{asset.Status}'.");

        if (request.Approve)
        {
            asset.Publish();
            logger.LogInformation("Asset approved by admin. AssetId={AssetId}", asset.Id);
        }
        else
        {
            asset.Archive();
            logger.LogInformation("Asset rejected by admin, archived. AssetId={AssetId} Reason={Reason}", asset.Id, request.Reason);
        }

        await repository.SaveAsync(asset, ct);

        return AssetMapper.ToModerationResponse(asset);
    }
}
