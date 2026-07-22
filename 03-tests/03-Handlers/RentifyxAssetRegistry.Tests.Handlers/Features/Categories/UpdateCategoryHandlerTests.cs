using RentifyxAssetRegistry.Application.Features.Categories;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Update;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Update.Request;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Update.Validator;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Interfaces.Category;
using ErrorOr;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Handlers.Features.Categories;

public sealed class UpdateCategoryHandlerTests
{
    [Fact]
    public async Task HandleAsync_AdminRenaming_UpdatesNameAndSaves()
    {
        CategoryEntity category = CategoryEntity.CreateRoot("Electronics");

        Mock<ICategoryRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);

        UpdateCategoryHandler handler = new(
            repository.Object,
            new UpdateCategoryValidator(),
            NullLogger<UpdateCategoryHandler>.Instance);

        ErrorOr<CategoryResponse> result = await handler.HandleAsync(
            new UpdateCategoryRequest(category.Id, true, "Consumer Electronics", null));

        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be("Consumer Electronics");
        repository.Verify(r => r.SaveAsync(category, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NonAdmin_ReturnsForbiddenWithoutSaving()
    {
        Mock<ICategoryRepository> repository = new();

        UpdateCategoryHandler handler = new(
            repository.Object,
            new UpdateCategoryValidator(),
            NullLogger<UpdateCategoryHandler>.Instance);

        ErrorOr<CategoryResponse> result = await handler.HandleAsync(
            new UpdateCategoryRequest(Guid.NewGuid(), false, "New Name", null));

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Forbidden);
        repository.Verify(r => r.SaveAsync(It.IsAny<CategoryEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_CategoryNotFound_ReturnsNotFound()
    {
        Mock<ICategoryRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoryEntity?)null);

        UpdateCategoryHandler handler = new(
            repository.Object,
            new UpdateCategoryValidator(),
            NullLogger<UpdateCategoryHandler>.Instance);

        ErrorOr<CategoryResponse> result = await handler.HandleAsync(
            new UpdateCategoryRequest(Guid.NewGuid(), true, "New Name", null));

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_ReParentCategoryWithChildren_ReturnsValidationErrorWithoutSaving()
    {
        CategoryEntity root = CategoryEntity.CreateRoot("Electronics");
        CategoryEntity child = CategoryEntity.CreateChild("Computers", root);
        CategoryEntity newParent = CategoryEntity.CreateRoot("Appliances");

        Mock<ICategoryRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(root.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(root);
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoryEntity> { root, child });

        UpdateCategoryHandler handler = new(
            repository.Object,
            new UpdateCategoryValidator(),
            NullLogger<UpdateCategoryHandler>.Instance);

        ErrorOr<CategoryResponse> result = await handler.HandleAsync(
            new UpdateCategoryRequest(root.Id, true, null, newParent.Id));

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        repository.Verify(r => r.SaveAsync(It.IsAny<CategoryEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ReParentLeafCategoryToItself_ReturnsValidationErrorWithoutSaving()
    {
        CategoryEntity category = CategoryEntity.CreateRoot("Electronics");

        Mock<ICategoryRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoryEntity> { category });

        UpdateCategoryHandler handler = new(
            repository.Object,
            new UpdateCategoryValidator(),
            NullLogger<UpdateCategoryHandler>.Instance);

        ErrorOr<CategoryResponse> result = await handler.HandleAsync(
            new UpdateCategoryRequest(category.Id, true, null, category.Id));

        result.IsError.Should().BeTrue();
        repository.Verify(r => r.SaveAsync(It.IsAny<CategoryEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ReParentLeafCategoryToValidParent_UpdatesParentAndSaves()
    {
        CategoryEntity category = CategoryEntity.CreateRoot("Electronics");
        CategoryEntity newParent = CategoryEntity.CreateRoot("Appliances");

        Mock<ICategoryRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(category.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(category);
        repository.Setup(r => r.GetByIdAsync(newParent.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newParent);
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoryEntity> { category, newParent });

        UpdateCategoryHandler handler = new(
            repository.Object,
            new UpdateCategoryValidator(),
            NullLogger<UpdateCategoryHandler>.Instance);

        ErrorOr<CategoryResponse> result = await handler.HandleAsync(
            new UpdateCategoryRequest(category.Id, true, null, newParent.Id));

        result.IsError.Should().BeFalse();
        result.Value.ParentCategoryId.Should().Be(newParent.Id);
        repository.Verify(r => r.SaveAsync(category, It.IsAny<CancellationToken>()), Times.Once);
    }
}
