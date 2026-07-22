using ErrorOr;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.AdminReviewAsset;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.AdminReviewAsset.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.AdminReviewAsset.Validator;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Enums;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;
using RentifyxAssetRegistry.Domain.ValueObjects;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Handlers.Features.Assets;

public sealed class AdminReviewAssetHandlerTests
{
    private static AssetEntity BuildPendingModerationAsset()
    {
        AssetEntity asset = AssetEntity.Create(
            Guid.NewGuid(),
            AssetTitle.Create("Excavator CAT 320"),
            AssetDescription.Create("Heavy duty excavator available for rent."),
            Money.Create(1000m),
            Guid.NewGuid(),
            Guid.NewGuid().ToString());
        asset.SubmitForModeration();
        return asset;
    }

    private static AdminReviewAssetHandler BuildHandler(Mock<IAssetRepository> repository)
        => new(repository.Object, new AdminReviewAssetValidator(), NullLogger<AdminReviewAssetHandler>.Instance);

    [Fact]
    public async Task HandleAsync_AdminApproves_TransitionsToActiveAndSaves()
    {
        AssetEntity asset = BuildPendingModerationAsset();
        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);

        AdminReviewAssetHandler handler = BuildHandler(repository);

        ErrorOr<AssetModerationResponse> result = await handler.HandleAsync(new AdminReviewAssetRequest(asset.Id, true, true));

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(AssetStatus.Active);
        repository.Verify(r => r.SaveAsync(asset, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AdminRejects_StaysPendingModerationWithoutSaving()
    {
        AssetEntity asset = BuildPendingModerationAsset();
        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);

        AdminReviewAssetHandler handler = BuildHandler(repository);

        ErrorOr<AssetModerationResponse> result = await handler.HandleAsync(new AdminReviewAssetRequest(asset.Id, false, true, "policy violation"));

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(AssetStatus.PendingModeration);
        repository.Verify(r => r.SaveAsync(It.IsAny<AssetEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NonAdminCaller_ReturnsForbiddenAndDoesNotTouchRepository()
    {
        Mock<IAssetRepository> repository = new();
        AdminReviewAssetHandler handler = BuildHandler(repository);

        ErrorOr<AssetModerationResponse> result = await handler.HandleAsync(new AdminReviewAssetRequest(Guid.NewGuid(), true, false));

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Forbidden);
        repository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AssetNotFound_ReturnsNotFound()
    {
        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((AssetEntity?)null);

        AdminReviewAssetHandler handler = BuildHandler(repository);

        ErrorOr<AssetModerationResponse> result = await handler.HandleAsync(new AdminReviewAssetRequest(Guid.NewGuid(), true, true));

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task HandleAsync_AssetNotPendingModeration_ReturnsValidationErrorAndDoesNotSave()
    {
        AssetEntity asset = BuildPendingModerationAsset();
        asset.Publish();

        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);

        AdminReviewAssetHandler handler = BuildHandler(repository);

        ErrorOr<AssetModerationResponse> result = await handler.HandleAsync(new AdminReviewAssetRequest(asset.Id, true, true));

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        repository.Verify(r => r.SaveAsync(It.IsAny<AssetEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
