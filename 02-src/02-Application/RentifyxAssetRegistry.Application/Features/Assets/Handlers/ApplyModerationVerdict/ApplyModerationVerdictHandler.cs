using ErrorOr;
using FluentValidation;
using Microsoft.Extensions.Logging;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Extensions;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ApplyModerationVerdict.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Mapper;
using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Enums;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;

namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.ApplyModerationVerdict;

public sealed class ApplyModerationVerdictHandler(
    IAssetRepository repository,
    IValidator<ApplyModerationVerdictRequest> validator,
    ILogger<ApplyModerationVerdictHandler> logger) : IHandler<ApplyModerationVerdictRequest, AssetModerationResponse>
{
    public async Task<ErrorOr<AssetModerationResponse>> HandleAsync(
        ApplyModerationVerdictRequest request,
        CancellationToken ct = default)
    {
        logger.LogInformation("Applying moderation verdict. Payload={@Payload}", request);

        List<Error>? errors = await validator.ValidateToErrorsAsync(request, ct);
        if (errors is not null)
            return errors;

        AssetEntity? asset = await repository.GetByIdAsync(request.AssetId, ct);
        if (asset is null)
            return Error.NotFound(AssetErrorCodes.NotFound, $"Asset {request.AssetId} not found.");

        if (asset.Status != AssetStatus.PendingModeration)
        {
            logger.LogInformation(
                "Verdict for AssetId={AssetId} ignored: asset is not PendingModeration (current status {Status}). Treated as an idempotent replay.",
                asset.Id, asset.Status);
            return AssetMapper.ToModerationResponse(asset);
        }

        if (request.Verdict == ModerationVerdict.Approved)
        {
            asset.Publish();
            await repository.SaveAsync(asset, ct);
            logger.LogInformation("Asset approved by moderation. AssetId={AssetId}", asset.Id);
        }
        else
        {
            logger.LogInformation("Asset held in PendingModeration. AssetId={AssetId} Verdict={Verdict}", asset.Id, request.Verdict);
        }

        return AssetMapper.ToModerationResponse(asset);
    }
}
