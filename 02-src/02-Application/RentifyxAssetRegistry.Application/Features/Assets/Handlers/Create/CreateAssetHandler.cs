using ErrorOr;
using FluentValidation;
using Microsoft.Extensions.Logging;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Extensions;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Mapper;
using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;
using RentifyxAssetRegistry.Domain.ValueObjects;

namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create;

public sealed class CreateAssetHandler(
    IAssetRepository repository,
    IOwnerStatusValidator ownerStatusValidator,
    IValidator<CreateAssetRequest> validator,
    ILogger<CreateAssetHandler> logger) : IHandler<CreateAssetRequest, CreateAssetResponse>
{
    public async Task<ErrorOr<CreateAssetResponse>> HandleAsync(
        CreateAssetRequest request,
        CancellationToken ct = default)
    {
        logger.LogInformation("Creating asset. Payload={@Payload}", request);

        List<Error>? errors = await validator.ValidateToErrorsAsync(request, ct);
        if (errors is not null)
            return errors;

        AssetEntity? existing = await repository.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct);
        if (existing is not null)
        {
            logger.LogInformation("Idempotent replay for key={IdempotencyKey}. AssetId={AssetId}", request.IdempotencyKey, existing.Id);
            return AssetMapper.ToCreateAssetResponse(existing);
        }

        bool isOwnerActive = await ownerStatusValidator.IsOwnerActiveAsync(request.OwnerId, ct);
        if (!isOwnerActive)
        {
            logger.LogWarning("Owner is not active. OwnerId={OwnerId}", request.OwnerId);
            return Error.Forbidden(AssetErrorCodes.OwnerNotActive, "Owner is not active and cannot create assets.");
        }

        AssetTitle title = AssetTitle.Create(request.Title);
        AssetDescription description = AssetDescription.Create(request.Description);
        Money price = Money.Create(request.Price);

        AssetEntity asset = AssetEntity.Create(
            request.OwnerId,
            title,
            description,
            price,
            request.CategoryId,
            request.IdempotencyKey);

        await repository.SaveAsync(asset, ct);

        logger.LogInformation("Asset created successfully. AssetId={AssetId}", asset.Id);

        return AssetMapper.ToCreateAssetResponse(asset);
    }
}
