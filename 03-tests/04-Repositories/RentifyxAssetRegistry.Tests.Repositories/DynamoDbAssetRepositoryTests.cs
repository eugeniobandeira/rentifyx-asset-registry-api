using FluentAssertions;
using RentifyxAssetRegistry.Domain.Common;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Enums;
using RentifyxAssetRegistry.Domain.Filters.Assets;
using RentifyxAssetRegistry.Domain.ValueObjects;
using RentifyxAssetRegistry.Infrastructure.Persistence;
using RentifyxAssetRegistry.Infrastructure.Persistence.Exceptions;
using RentifyxAssetRegistry.Tests.Repositories.Fixtures;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Repositories;

[Collection(LocalStackFixtureGroup.Name)]
public sealed class DynamoDbAssetRepositoryTests(LocalStackFixture fixture)
{
    private readonly DynamoDbAssetRepository _repository = new(fixture.Client, fixture.Context, fixture.Options);

    [Fact]
    public async Task SaveAsync_NewAsset_PersistsAndIsRetrievableById()
    {
        AssetEntity asset = CreateAsset(out _);

        await _repository.SaveAsync(asset);
        AssetEntity? loaded = await _repository.GetByIdAsync(asset.Id);

        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(asset.Id);
        loaded.Title.Value.Should().Be(asset.Title.Value);
        loaded.Status.Should().Be(AssetStatus.Draft);
    }

    [Fact]
    public async Task SaveAsync_EntityWithDomainEvents_ClearsDomainEventsAfterPersisting()
    {
        AssetEntity asset = CreateAsset(out _);
        asset.DomainEvents.Should().NotBeEmpty();

        await _repository.SaveAsync(asset);

        asset.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_UnknownId_ReturnsNull()
    {
        AssetEntity? loaded = await _repository.GetByIdAsync(Guid.NewGuid());

        loaded.Should().BeNull();
    }

    [Fact]
    public async Task GetByOwnerAsync_ReturnsAssetsForThatOwner()
    {
        Guid ownerId = Guid.NewGuid();
        AssetEntity asset = CreateAsset(out _, ownerId: ownerId);
        await _repository.SaveAsync(asset);

        IReadOnlyList<AssetEntity> results = await _repository.GetByOwnerAsync(ownerId);

        results.Should().Contain(a => a.Id == asset.Id);
    }

    [Fact]
    public async Task GetByIdempotencyKeyAsync_KnownKey_ReturnsAsset()
    {
        string idempotencyKey = Guid.NewGuid().ToString();
        AssetEntity asset = CreateAsset(out _, idempotencyKey: idempotencyKey);
        await _repository.SaveAsync(asset);

        AssetEntity? found = await _repository.GetByIdempotencyKeyAsync(idempotencyKey);

        found.Should().NotBeNull();
        found!.Id.Should().Be(asset.Id);
    }

    [Fact]
    public async Task GetByIdempotencyKeyAsync_UnknownKey_ReturnsNull()
    {
        AssetEntity? found = await _repository.GetByIdempotencyKeyAsync(Guid.NewGuid().ToString());

        found.Should().BeNull();
    }

    [Fact]
    public async Task SoftDeleteAsync_FlipsStatusToArchived()
    {
        AssetEntity asset = CreateAsset(out _);
        await _repository.SaveAsync(asset);

        await _repository.SoftDeleteAsync(asset.Id);
        AssetEntity? reloaded = await _repository.GetByIdAsync(asset.Id);

        reloaded.Should().NotBeNull();
        reloaded!.Status.Should().Be(AssetStatus.Archived);
    }

    [Fact]
    public async Task SearchAsync_ByCategory_ReturnsOnlyActiveAssetsInThatCategory()
    {
        Guid categoryId = Guid.NewGuid();
        AssetEntity asset = CreateAsset(out _, categoryId: categoryId);
        asset.SubmitForModeration();
        asset.Publish();
        await _repository.SaveAsync(asset);

        AssetSearchFilter filter = new(10, AssetStatus.Active, CategoryId: categoryId);
        CursorPagedResult<AssetEntity> result = await _repository.SearchAsync(filter);

        result.Items.Should().Contain(a => a.Id == asset.Id);
    }

    [Fact]
    public async Task SearchAsync_ByStatusOnly_ReturnsActiveAssets()
    {
        AssetEntity asset = CreateAsset(out _);
        asset.SubmitForModeration();
        asset.Publish();
        await _repository.SaveAsync(asset);

        AssetSearchFilter filter = new(10, AssetStatus.Active);
        CursorPagedResult<AssetEntity> result = await _repository.SearchAsync(filter);

        result.Items.Should().Contain(a => a.Id == asset.Id);
    }

    [Fact]
    public async Task SearchAsync_WithPriceRangeOutsideAsset_ExcludesAsset()
    {
        AssetEntity asset = CreateAsset(out _, price: 50m);
        asset.SubmitForModeration();
        asset.Publish();
        await _repository.SaveAsync(asset);

        AssetSearchFilter filter = new(10, AssetStatus.Active, MinPrice: 100m, MaxPrice: 200m);
        CursorPagedResult<AssetEntity> result = await _repository.SearchAsync(filter);

        result.Items.Should().NotContain(a => a.Id == asset.Id);
    }

    [Fact]
    public async Task SearchAsync_WithKeywordMatchingTitle_ReturnsAsset()
    {
        string uniqueWord = $"Zyx{Guid.NewGuid():N}";
        AssetEntity asset = CreateAsset(out _, title: $"Asset {uniqueWord} Title");
        asset.SubmitForModeration();
        asset.Publish();
        await _repository.SaveAsync(asset);

        AssetSearchFilter filter = new(10, AssetStatus.Active, Keyword: uniqueWord);
        CursorPagedResult<AssetEntity> result = await _repository.SearchAsync(filter);

        result.Items.Should().Contain(a => a.Id == asset.Id);
    }

    [Fact]
    public async Task SearchAsync_MalformedPageToken_ThrowsInvalidPageTokenException()
    {
        AssetSearchFilter filter = new(10, AssetStatus.Active, NextPageToken: "not-a-valid-token!!");

        Func<Task> act = async () => await _repository.SearchAsync(filter);

        await act.Should().ThrowAsync<InvalidPageTokenException>();
    }

    [Fact]
    public async Task SaveAsync_MoreThanNinetyNineDomainEvents_ThrowsInvalidOperationException()
    {
        AssetEntity asset = CreateAsset(out _);

        for (int i = 0; i < 100; i++)
            asset.AttachMedia(Media.Create($"key-{i}", "image/png", 1024, MediaUploadStatus.Uploaded));

        Func<Task> act = async () => await _repository.SaveAsync(asset);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static AssetEntity CreateAsset(
        out Guid ownerIdOut,
        Guid? ownerId = null,
        Guid? categoryId = null,
        string? idempotencyKey = null,
        decimal price = 100m,
        string title = "Sample Asset Title")
    {
        ownerIdOut = ownerId ?? Guid.NewGuid();

        return AssetEntity.Create(
            ownerIdOut,
            AssetTitle.Create(title),
            AssetDescription.Create("A sufficiently long description for validation purposes."),
            Money.Create(price),
            categoryId ?? Guid.NewGuid(),
            idempotencyKey ?? Guid.NewGuid().ToString());
    }
}
