using ErrorOr;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.SubmitForModeration;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.SubmitForModeration.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.SubmitForModeration.Validator;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Enums;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;
using RentifyxAssetRegistry.Domain.ValueObjects;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Handlers.Features.Assets;

public sealed class SubmitForModerationHandlerTests
{
    private static AssetEntity BuildDraftAsset(Guid ownerId) => AssetEntity.Create(
        ownerId,
        AssetTitle.Create("Excavator CAT 320"),
        AssetDescription.Create("Heavy duty excavator available for rent."),
        Money.Create(1000m),
        Guid.NewGuid(),
        Guid.NewGuid().ToString());

    private static SubmitForModerationHandler BuildHandler(Mock<IAssetRepository> repository)
        => new(repository.Object, new SubmitForModerationValidator(), NullLogger<SubmitForModerationHandler>.Instance);

    [Fact]
    public async Task HandleAsync_DraftAssetOwnedByCaller_TransitionsToPendingModeration()
    {
        Guid ownerId = Guid.NewGuid();
        AssetEntity asset = BuildDraftAsset(ownerId);

        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);

        SubmitForModerationHandler handler = BuildHandler(repository);

        ErrorOr<AssetModerationResponse> result = await handler.HandleAsync(new SubmitForModerationRequest(asset.Id, ownerId));

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(AssetStatus.PendingModeration);
        repository.Verify(r => r.SaveAsync(asset, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AssetNotFound_ReturnsNotFound()
    {
        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((AssetEntity?)null);

        SubmitForModerationHandler handler = BuildHandler(repository);

        ErrorOr<AssetModerationResponse> result = await handler.HandleAsync(new SubmitForModerationRequest(Guid.NewGuid(), Guid.NewGuid()));

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_NonOwnerCaller_ReturnsForbiddenAndDoesNotSave()
    {
        AssetEntity asset = BuildDraftAsset(Guid.NewGuid());

        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);

        SubmitForModerationHandler handler = BuildHandler(repository);

        ErrorOr<AssetModerationResponse> result = await handler.HandleAsync(new SubmitForModerationRequest(asset.Id, Guid.NewGuid()));

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Forbidden);
        repository.Verify(r => r.SaveAsync(It.IsAny<AssetEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AssetNotDraft_ReturnsValidationErrorAndDoesNotSave()
    {
        Guid ownerId = Guid.NewGuid();
        AssetEntity asset = BuildDraftAsset(ownerId);
        asset.SubmitForModeration();

        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);

        SubmitForModerationHandler handler = BuildHandler(repository);

        ErrorOr<AssetModerationResponse> result = await handler.HandleAsync(new SubmitForModerationRequest(asset.Id, ownerId));

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        repository.Verify(r => r.SaveAsync(It.IsAny<AssetEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_InvalidRequest_ReturnsValidationErrorsWithoutTouchingRepository()
    {
        Mock<IAssetRepository> repository = new();
        SubmitForModerationHandler handler = BuildHandler(repository);

        ErrorOr<AssetModerationResponse> result = await handler.HandleAsync(new SubmitForModerationRequest(Guid.Empty, Guid.Empty));

        result.IsError.Should().BeTrue();
        repository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
