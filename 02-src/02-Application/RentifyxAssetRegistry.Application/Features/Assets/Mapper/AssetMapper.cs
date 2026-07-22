using RentifyxAssetRegistry.Domain.Common;
using RentifyxAssetRegistry.Domain.Entities;

namespace RentifyxAssetRegistry.Application.Features.Assets.Mapper;

public static class AssetMapper
{
    public static CreateAssetResponse ToCreateAssetResponse(AssetEntity entity)
        => new(entity.Id, entity.Status, entity.CreatedAt);

    public static SearchAssetsResponse ToSearchAssetsResponse(CursorPagedResult<AssetEntity> result)
        => new(result.Items.Select(ToAssetSummaryResponse).ToList(), result.NextPageToken);

    public static AssetSummaryResponse ToAssetSummaryResponse(AssetEntity entity)
        => new(entity.Id, entity.Title.Value, entity.Price.Amount, entity.CategoryId, entity.Status);
}
