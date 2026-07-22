using ErrorOr;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Search;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Search.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Search.Validator;
using RentifyxAssetRegistry.Domain.Common;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Enums;
using RentifyxAssetRegistry.Domain.Filters.Assets;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;
using RentifyxAssetRegistry.Domain.ValueObjects;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Handlers.Features.Assets;

public sealed class SearchAssetsHandlerTests
{
    private static AssetEntity BuildAsset() => AssetEntity.Create(
        Guid.NewGuid(),
        AssetTitle.Create("Excavator CAT 320"),
        AssetDescription.Create("Heavy duty excavator available for rent."),
        Money.Create(1000m),
        Guid.NewGuid(),
        Guid.NewGuid().ToString());

    private static SearchAssetsHandler BuildHandler(Mock<IAssetRepository> repository)
        => new(repository.Object, new SearchAssetsValidator(), NullLogger<SearchAssetsHandler>.Instance);

    [Fact]
    public async Task HandleAsync_ValidRequest_ReturnsMappedItemsAndToken()
    {
        AssetEntity asset = BuildAsset();
        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.SearchAsync(It.IsAny<AssetSearchFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CursorPagedResult<AssetEntity>([asset], "next-token"));

        SearchAssetsHandler handler = BuildHandler(repository);

        ErrorOr<SearchAssetsResponse> result = await handler.HandleAsync(new SearchAssetsRequest(PageSize: 10));

        result.IsError.Should().BeFalse();
        result.Value.Items.Should().ContainSingle(i => i.Id == asset.Id);
        result.Value.NextPageToken.Should().Be("next-token");
    }

    [Fact]
    public async Task HandleAsync_AlwaysFiltersByActiveStatus()
    {
        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.SearchAsync(It.IsAny<AssetSearchFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CursorPagedResult<AssetEntity>([], null));

        SearchAssetsHandler handler = BuildHandler(repository);

        await handler.HandleAsync(new SearchAssetsRequest(PageSize: 10));

        repository.Verify(r => r.SearchAsync(
            It.Is<AssetSearchFilter>(f => f.Status == AssetStatus.Active),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_BlankKeyword_PassesNullKeywordToRepository()
    {
        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.SearchAsync(It.IsAny<AssetSearchFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CursorPagedResult<AssetEntity>([], null));

        SearchAssetsHandler handler = BuildHandler(repository);

        await handler.HandleAsync(new SearchAssetsRequest(PageSize: 10, Keyword: "   "));

        repository.Verify(r => r.SearchAsync(
            It.Is<AssetSearchFilter>(f => f.Keyword == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_InvalidRequest_ReturnsValidationErrorsWithoutTouchingRepository()
    {
        Mock<IAssetRepository> repository = new();
        SearchAssetsHandler handler = BuildHandler(repository);

        ErrorOr<SearchAssetsResponse> result = await handler.HandleAsync(
            new SearchAssetsRequest(PageSize: 0, MinPrice: 100m, MaxPrice: 50m));

        result.IsError.Should().BeTrue();
        repository.Verify(r => r.SearchAsync(It.IsAny<AssetSearchFilter>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NoMatches_ReturnsEmptyItemsWithNullToken()
    {
        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.SearchAsync(It.IsAny<AssetSearchFilter>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CursorPagedResult<AssetEntity>([], null));

        SearchAssetsHandler handler = BuildHandler(repository);

        ErrorOr<SearchAssetsResponse> result = await handler.HandleAsync(new SearchAssetsRequest(PageSize: 10));

        result.IsError.Should().BeFalse();
        result.Value.Items.Should().BeEmpty();
        result.Value.NextPageToken.Should().BeNull();
    }
}
