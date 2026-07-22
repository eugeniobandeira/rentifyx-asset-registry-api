using ErrorOr;
using FluentValidation;
using Microsoft.Extensions.Logging;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Extensions;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Update.Request;
using RentifyxAssetRegistry.Application.Features.Categories.Mapper;
using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Interfaces.Category;

namespace RentifyxAssetRegistry.Application.Features.Categories.Handlers.Update;

public sealed class UpdateCategoryHandler(
    ICategoryRepository repository,
    IValidator<UpdateCategoryRequest> validator,
    ILogger<UpdateCategoryHandler> logger) : IHandler<UpdateCategoryRequest, CategoryResponse>
{
    public async Task<ErrorOr<CategoryResponse>> HandleAsync(
        UpdateCategoryRequest request,
        CancellationToken ct = default)
    {
        logger.LogInformation("Updating category. Payload={@Payload}", request);

        List<Error>? errors = await validator.ValidateToErrorsAsync(request, ct);
        if (errors is not null)
            return errors;

        if (!request.IsAdmin)
            return Error.Forbidden(CategoryErrorCodes.NotAdmin, "Only admins can update categories.");

        CategoryEntity? category = await repository.GetByIdAsync(request.CategoryId, ct);
        if (category is null)
            return Error.NotFound(CategoryErrorCodes.NotFound, $"Category {request.CategoryId} not found.");

        if (request.NewName is not null)
            category.Rename(request.NewName);

        if (request.NewParentCategoryId is not null && request.NewParentCategoryId != category.ParentCategoryId)
        {
            IReadOnlyList<CategoryEntity> allCategories = await repository.GetAllAsync(ct);
            bool hasChildren = allCategories.Any(c => c.ParentCategoryId == category.Id);
            if (hasChildren)
                return Error.Validation(CategoryErrorCodes.HasChildren, "Cannot re-parent a category that has children.");

            CategoryEntity? newParent = await repository.GetByIdAsync(request.NewParentCategoryId.Value, ct);
            if (newParent is null)
                return Error.NotFound(CategoryErrorCodes.NotFound, $"Category {request.NewParentCategoryId} not found.");

            if (newParent.Id == category.Id)
                return Error.Validation(CategoryErrorCodes.SelfParent, "A category cannot be re-parented to itself.");

            if (!newParent.CanAcceptChild)
                return Error.Validation(CategoryErrorCodes.MaxDepthExceeded, $"Cannot re-parent under a category at depth {newParent.Depth}.");

            category.ReParent(newParent);
        }

        await repository.SaveAsync(category, ct);

        logger.LogInformation("Category updated successfully. CategoryId={CategoryId}", category.Id);

        return CategoryMapper.ToResponse(category);
    }
}
