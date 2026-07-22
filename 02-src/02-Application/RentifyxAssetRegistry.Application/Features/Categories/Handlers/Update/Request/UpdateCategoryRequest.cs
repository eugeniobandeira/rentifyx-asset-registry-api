namespace RentifyxAssetRegistry.Application.Features.Categories.Handlers.Update.Request;

public sealed record UpdateCategoryRequest(
    Guid CategoryId,
    bool IsAdmin,
    string? NewName,
    Guid? NewParentCategoryId
);
