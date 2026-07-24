using ErrorOr;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.GetById;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.GetById.Request;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;
using RentifyxAssetRegistry.Domain.ValueObjects;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Handlers.Features.Assets;

public sealed class GetAssetByIdHandlerTests
{
    private static AssetEntity BuildAsset() => AssetEntity.Create(
        Guid.NewGuid(),
        AssetTitle.Create("Excavator CAT 320"),
        AssetDescription.Create("Heavy duty excavator available for rent."),
        Money.Create(1000m),
        Guid.NewGuid(),
        Guid.NewGuid().ToString());

    private static GetAssetByIdHandler BuildHandler(Mock<IAssetRepository> repository)
        => new(repository.Object, NullLogger<GetAssetByIdHandler>.Instance);

    [Fact]
    public async Task HandleAsync_AssetExists_ReturnsMappedResponse()
    {
        AssetEntity asset = BuildAsset();

        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);

        GetAssetByIdHandler handler = BuildHandler(repository);

        ErrorOr<GetAssetByIdResponse> result = await handler.HandleAsync(new GetAssetByIdRequest(asset.Id));

        result.IsError.Should().BeFalse();
        result.Value.Id.Should().Be(asset.Id);
        result.Value.Title.Should().Be(asset.Title.Value);
        result.Value.Description.Should().Be(asset.Description.Value);
        result.Value.Price.Should().Be(asset.Price.Amount);
        result.Value.CategoryId.Should().Be(asset.CategoryId);
        result.Value.OwnerId.Should().Be(asset.OwnerId);
        result.Value.Status.Should().Be(asset.Status);
        result.Value.CreatedAt.Should().Be(asset.CreatedAt);
    }

    [Fact]
    public async Task HandleAsync_AssetNotFound_ReturnsNotFound()
    {
        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((AssetEntity?)null);

        GetAssetByIdHandler handler = BuildHandler(repository);

        ErrorOr<GetAssetByIdResponse> result = await handler.HandleAsync(new GetAssetByIdRequest(Guid.NewGuid()));

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }
}
