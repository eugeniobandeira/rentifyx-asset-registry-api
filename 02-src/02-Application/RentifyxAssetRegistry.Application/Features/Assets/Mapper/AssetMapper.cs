using RentifyxAssetRegistry.Domain.Entities;

namespace RentifyxAssetRegistry.Application.Features.Assets.Mapper;

public static class AssetMapper
{
    public static CreateAssetResponse ToCreateAssetResponse(AssetEntity entity)
        => new(entity.Id, entity.Status, entity.CreatedAt);
}
