namespace RentifyxAssetRegistry.Application.Features.Categories.Handlers.Create.Request;

public sealed record CreateCategoryRequest(
    string Name,
    Guid? ParentCategoryId,
    bool IsAdmin
);
