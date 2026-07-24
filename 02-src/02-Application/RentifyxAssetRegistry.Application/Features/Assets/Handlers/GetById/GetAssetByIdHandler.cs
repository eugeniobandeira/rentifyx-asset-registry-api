using ErrorOr;
using Microsoft.Extensions.Logging;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.GetById.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Mapper;
using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;

namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.GetById;

public sealed class GetAssetByIdHandler(
    IAssetRepository repository,
    ILogger<GetAssetByIdHandler> logger) : IHandler<GetAssetByIdRequest, GetAssetByIdResponse>
{
    public async Task<ErrorOr<GetAssetByIdResponse>> HandleAsync(
        GetAssetByIdRequest request,
        CancellationToken ct = default)
    {
        AssetEntity? asset = await repository.GetByIdAsync(request.AssetId, ct);
        if (asset is null)
        {
            logger.LogWarning("Asset not found. AssetId={AssetId}", request.AssetId);
            return Error.NotFound(AssetErrorCodes.NotFound, $"Asset {request.AssetId} not found.");
        }

        return AssetMapper.ToGetByIdResponse(asset);
    }
}
