using RentifyxAssetRegistry.Application.Features.Categories;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.ListCategories;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.ListCategories.Request;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Interfaces.Category;
using ErrorOr;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Handlers.Features.Categories;

public sealed class ListCategoriesHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsAllCategoriesMapped()
    {
        CategoryEntity root = CategoryEntity.CreateRoot("Electronics");
        CategoryEntity child = CategoryEntity.CreateChild("Computers", root);

        Mock<ICategoryRepository> repository = new();
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoryEntity> { root, child });

        ListCategoriesHandler handler = new(repository.Object, NullLogger<ListCategoriesHandler>.Instance);

        ErrorOr<IReadOnlyList<CategoryResponse>> result = await handler.HandleAsync(new ListCategoriesRequest());

        result.IsError.Should().BeFalse();
        result.Value.Should().HaveCount(2);
    }
}
