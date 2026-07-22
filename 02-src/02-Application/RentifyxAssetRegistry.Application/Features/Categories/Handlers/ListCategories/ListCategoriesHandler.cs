using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.ListCategories.Request;
using RentifyxAssetRegistry.Application.Features.Categories.Mapper;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Interfaces.Category;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace RentifyxAssetRegistry.Application.Features.Categories.Handlers.ListCategories;

public sealed class ListCategoriesHandler(
    ICategoryRepository repository,
    ILogger<ListCategoriesHandler> logger) : IHandler<ListCategoriesRequest, IReadOnlyList<CategoryResponse>>
{
    public async Task<ErrorOr<IReadOnlyList<CategoryResponse>>> HandleAsync(
        ListCategoriesRequest request,
        CancellationToken ct = default)
    {
        logger.LogDebug("Fetching categories.");

        IReadOnlyList<CategoryEntity> categories = await repository.GetAllAsync(ct);

        logger.LogDebug("Fetched {Count} categories.", categories.Count);

        return categories.Select(CategoryMapper.ToResponse).ToList();
    }
}
