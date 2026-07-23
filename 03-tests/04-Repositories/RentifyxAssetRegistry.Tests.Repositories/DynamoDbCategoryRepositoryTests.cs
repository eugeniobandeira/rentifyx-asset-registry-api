using FluentAssertions;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Infrastructure.Persistence;
using RentifyxAssetRegistry.Tests.Repositories.Fixtures;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Repositories;

[Collection(LocalStackFixtureGroup.Name)]
public sealed class DynamoDbCategoryRepositoryTests(LocalStackFixture fixture)
{
    private readonly DynamoDbCategoryRepository _repository = new(fixture.Client, fixture.Context, fixture.Options);

    [Fact]
    public async Task SaveAsync_NewRootCategory_PersistsAndIsRetrievableById()
    {
        CategoryEntity category = CategoryEntity.CreateRoot($"Root-{Guid.NewGuid():N}");

        await _repository.SaveAsync(category);
        CategoryEntity? loaded = await _repository.GetByIdAsync(category.Id);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(category.Id);
        loaded.Name.Should().Be(category.Name);
        loaded.ParentCategoryId.Should().BeNull();
        loaded.Depth.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        CategoryEntity? loaded = await _repository.GetByIdAsync(Guid.NewGuid());

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_ChildCategory_PersistsParentLinkAndDepth()
    {
        CategoryEntity parent = CategoryEntity.CreateRoot($"Parent-{Guid.NewGuid():N}");
        await _repository.SaveAsync(parent);

        CategoryEntity child = CategoryEntity.CreateChild($"Child-{Guid.NewGuid():N}", parent);
        await _repository.SaveAsync(child);

        CategoryEntity? loaded = await _repository.GetByIdAsync(child.Id);

        loaded.Should().NotBeNull();
        loaded!.ParentCategoryId.Should().Be(parent.Id);
        loaded.Depth.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsPersistedCategories()
    {
        CategoryEntity category = CategoryEntity.CreateRoot($"Listed-{Guid.NewGuid():N}");
        await _repository.SaveAsync(category);

        IReadOnlyList<CategoryEntity> all = await _repository.GetAllAsync();

        all.Should().Contain(c => c.Id == category.Id);
    }

    [Fact]
    public async Task SaveAsync_RenamedCategory_UpsertsInPlace()
    {
        CategoryEntity category = CategoryEntity.CreateRoot($"Original-{Guid.NewGuid():N}");
        await _repository.SaveAsync(category);

        category.Rename($"Renamed-{Guid.NewGuid():N}");
        await _repository.SaveAsync(category);

        CategoryEntity? loaded = await _repository.GetByIdAsync(category.Id);

        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be(category.Name);
    }
}
