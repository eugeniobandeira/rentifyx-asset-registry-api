using ErrorOr;
using FluentValidation;
using Microsoft.Extensions.Logging;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Extensions;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Create.Request;
using RentifyxAssetRegistry.Application.Features.Categories.Mapper;
using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Interfaces.Category;

namespace RentifyxAssetRegistry.Application.Features.Categories.Handlers.Create;

public sealed class CreateCategoryHandler(
    ICategoryRepository repository,
    IValidator<CreateCategoryRequest> validator,
    ILogger<CreateCategoryHandler> logger) : IHandler<CreateCategoryRequest, CategoryResponse>
{
    public async Task<ErrorOr<CategoryResponse>> HandleAsync(
        CreateCategoryRequest request,
        CancellationToken ct = default)
    {
        logger.LogInformation("Creating category. Payload={@Payload}", request);

        List<Error>? errors = await validator.ValidateToErrorsAsync(request, ct);
        if (errors is not null)
            return errors;

        if (!request.IsAdmin)
            return Error.Forbidden(CategoryErrorCodes.NotAdmin, "Only admins can create categories.");

        CategoryEntity category;

        if (request.ParentCategoryId is null)
        {
            category = CategoryEntity.CreateRoot(request.Name);
        }
        else
        {
            CategoryEntity? parent = await repository.GetByIdAsync(request.ParentCategoryId.Value, ct);
            if (parent is null)
                return Error.NotFound(CategoryErrorCodes.NotFound, $"Category {request.ParentCategoryId} not found.");

            if (!parent.CanAcceptChild)
                return Error.Validation(CategoryErrorCodes.MaxDepthExceeded, $"Cannot create a child category under a parent at depth {parent.Depth}.");

            category = CategoryEntity.CreateChild(request.Name, parent);
        }

        await repository.SaveAsync(category, ct);

        logger.LogInformation("Category created successfully. CategoryId={CategoryId}", category.Id);

        return CategoryMapper.ToResponse(category);
    }
}
