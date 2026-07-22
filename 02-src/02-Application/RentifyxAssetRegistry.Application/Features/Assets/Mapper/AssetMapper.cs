using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create.Request;
using RentifyxAssetRegistry.Domain.Common;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.ValueObjects;

namespace RentifyxAssetRegistry.Application.Features.Assets.Mapper;

public static class AssetMapper
{
    public static AssetEntity ToNewAsset(CreateAssetRequest request)
        => AssetEntity.Create(
            request.OwnerId,
            AssetTitle.Create(request.Title),
            AssetDescription.Create(request.Description),
            Money.Create(request.Price),
            request.CategoryId,
            request.IdempotencyKey);

    public static CreateAssetResponse ToCreateAssetResponse(AssetEntity entity)
        => new(entity.Id, entity.Status, entity.CreatedAt);

    public static SearchAssetsResponse ToSearchAssetsResponse(CursorPagedResult<AssetEntity> result)
        => new(result.Items.Select(ToAssetSummaryResponse).ToList(), result.NextPageToken);

    public static AssetSummaryResponse ToAssetSummaryResponse(AssetEntity entity)
        => new(entity.Id, entity.Title.Value, entity.Price.Amount, entity.CategoryId, entity.Status);

    public static AssetModerationResponse ToModerationResponse(AssetEntity entity)
        => new(entity.Id, entity.Status);
}
