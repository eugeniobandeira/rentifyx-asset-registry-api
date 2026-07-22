namespace RentifyxAssetRegistry.Application.Features.Categories;

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    Guid? ParentCategoryId,
    int Depth
);
