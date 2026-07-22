using ErrorOr;
using FluentValidation;
using Microsoft.Extensions.Logging;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Extensions;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.SubmitForModeration.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Mapper;
using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Enums;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;

namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.SubmitForModeration;

public sealed class SubmitForModerationHandler(
    IAssetRepository repository,
    IValidator<SubmitForModerationRequest> validator,
    ILogger<SubmitForModerationHandler> logger) : IHandler<SubmitForModerationRequest, AssetModerationResponse>
{
    public async Task<ErrorOr<AssetModerationResponse>> HandleAsync(
        SubmitForModerationRequest request,
        CancellationToken ct = default)
    {
        logger.LogInformation("Submitting asset for moderation. Payload={@Payload}", request);

        List<Error>? errors = await validator.ValidateToErrorsAsync(request, ct);
        if (errors is not null)
            return errors;

        AssetEntity? asset = await repository.GetByIdAsync(request.AssetId, ct);
        if (asset is null)
            return Error.NotFound(AssetErrorCodes.NotFound, $"Asset {request.AssetId} not found.");

        if (asset.OwnerId != request.OwnerId)
            return Error.Forbidden(AssetErrorCodes.NotOwner, "OwnerId does not match the asset's owner.");

        if (asset.Status != AssetStatus.Draft)
            return Error.Validation(AssetErrorCodes.InvalidStatus, $"Asset must be in 'Draft' status. Current status is '{asset.Status}'.");

        asset.SubmitForModeration();

        await repository.SaveAsync(asset, ct);

        logger.LogInformation("Asset submitted for moderation. AssetId={AssetId}", asset.Id);

        return AssetMapper.ToModerationResponse(asset);
    }
}
