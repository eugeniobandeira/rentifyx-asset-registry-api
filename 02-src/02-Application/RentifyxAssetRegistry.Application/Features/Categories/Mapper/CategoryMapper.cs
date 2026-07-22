using RentifyxAssetRegistry.Domain.Entities;

namespace RentifyxAssetRegistry.Application.Features.Categories.Mapper;

public static class CategoryMapper
{
    public static CategoryResponse ToResponse(CategoryEntity entity)
        => new(entity.Id, entity.Name, entity.ParentCategoryId, entity.Depth);
}
