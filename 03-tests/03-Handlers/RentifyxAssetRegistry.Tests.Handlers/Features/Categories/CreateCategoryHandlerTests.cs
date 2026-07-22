using RentifyxAssetRegistry.Application.Features.Categories;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Create;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Create.Request;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Create.Validator;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Interfaces.Category;
using ErrorOr;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Handlers.Features.Categories;

public sealed class CreateCategoryHandlerTests
{
    [Fact]
    public async Task HandleAsync_AdminCreatingRoot_SavesAndReturnsResponse()
    {
        Mock<ICategoryRepository> repository = new();

        CreateCategoryHandler handler = new(
            repository.Object,
            new CreateCategoryValidator(),
            NullLogger<CreateCategoryHandler>.Instance);

        ErrorOr<CategoryResponse> result = await handler.HandleAsync(new CreateCategoryRequest("Electronics", null, true));

        result.IsError.Should().BeFalse();
        result.Value.Depth.Should().Be(1);
        repository.Verify(r => r.SaveAsync(It.IsAny<CategoryEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NonAdmin_ReturnsForbiddenWithoutSaving()
    {
        Mock<ICategoryRepository> repository = new();

        CreateCategoryHandler handler = new(
            repository.Object,
            new CreateCategoryValidator(),
            NullLogger<CreateCategoryHandler>.Instance);

        ErrorOr<CategoryResponse> result = await handler.HandleAsync(new CreateCategoryRequest("Electronics", null, false));

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Forbidden);
        repository.Verify(r => r.SaveAsync(It.IsAny<CategoryEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ParentNotFound_ReturnsNotFoundWithoutSaving()
    {
        Mock<ICategoryRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoryEntity?)null);

        CreateCategoryHandler handler = new(
            repository.Object,
            new CreateCategoryValidator(),
            NullLogger<CreateCategoryHandler>.Instance);

        ErrorOr<CategoryResponse> result = await handler.HandleAsync(new CreateCategoryRequest("Computers", Guid.NewGuid(), true));

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        repository.Verify(r => r.SaveAsync(It.IsAny<CategoryEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ParentAtMaxDepth_ReturnsValidationErrorWithoutSaving()
    {
        CategoryEntity root = CategoryEntity.CreateRoot("Electronics");
        CategoryEntity depthTwo = CategoryEntity.CreateChild("Computers", root);
        CategoryEntity depthThree = CategoryEntity.CreateChild("Laptops", depthTwo);

        Mock<ICategoryRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(depthThree.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(depthThree);

        CreateCategoryHandler handler = new(
            repository.Object,
            new CreateCategoryValidator(),
            NullLogger<CreateCategoryHandler>.Instance);

        ErrorOr<CategoryResponse> result = await handler.HandleAsync(new CreateCategoryRequest("Gaming Laptops", depthThree.Id, true));

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        repository.Verify(r => r.SaveAsync(It.IsAny<CategoryEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
