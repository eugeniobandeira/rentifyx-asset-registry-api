using ErrorOr;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ApplyModerationVerdict;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ApplyModerationVerdict.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ApplyModerationVerdict.Validator;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Enums;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;
using RentifyxAssetRegistry.Domain.ValueObjects;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Handlers.Features.Assets;

public sealed class ApplyModerationVerdictHandlerTests
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

    private static ApplyModerationVerdictHandler BuildHandler(Mock<IAssetRepository> repository)
        => new(repository.Object, new ApplyModerationVerdictValidator(), NullLogger<ApplyModerationVerdictHandler>.Instance);

    [Fact]
    public async Task HandleAsync_ApprovedVerdict_TransitionsToActiveAndSaves()
    {
        AssetEntity asset = BuildPendingModerationAsset();
        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);

        ApplyModerationVerdictHandler handler = BuildHandler(repository);

        ErrorOr<AssetModerationResponse> result = await handler.HandleAsync(new ApplyModerationVerdictRequest(asset.Id, ModerationVerdict.Approved));

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(AssetStatus.Active);
        repository.Verify(r => r.SaveAsync(asset, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_RejectedVerdict_ArchivesAndSaves()
    {
        AssetEntity asset = BuildPendingModerationAsset();
        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);

        ApplyModerationVerdictHandler handler = BuildHandler(repository);

        ErrorOr<AssetModerationResponse> result = await handler.HandleAsync(new ApplyModerationVerdictRequest(asset.Id, ModerationVerdict.Rejected));

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(AssetStatus.Archived);
        repository.Verify(r => r.SaveAsync(asset, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_PendingReviewVerdict_StaysPendingModerationWithoutSaving()
    {
        AssetEntity asset = BuildPendingModerationAsset();
        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);

        ApplyModerationVerdictHandler handler = BuildHandler(repository);

        ErrorOr<AssetModerationResponse> result = await handler.HandleAsync(new ApplyModerationVerdictRequest(asset.Id, ModerationVerdict.PendingReview));

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(AssetStatus.PendingModeration);
        repository.Verify(r => r.SaveAsync(It.IsAny<AssetEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AssetAlreadyActive_IsIdempotentNoOp()
    {
        AssetEntity asset = BuildPendingModerationAsset();
        asset.Publish();

        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>())).ReturnsAsync(asset);

        ApplyModerationVerdictHandler handler = BuildHandler(repository);

        ErrorOr<AssetModerationResponse> result = await handler.HandleAsync(new ApplyModerationVerdictRequest(asset.Id, ModerationVerdict.Approved));

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(AssetStatus.Active);
        repository.Verify(r => r.SaveAsync(It.IsAny<AssetEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AssetNotFound_ReturnsNotFound()
    {
        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((AssetEntity?)null);

        ApplyModerationVerdictHandler handler = BuildHandler(repository);

        ErrorOr<AssetModerationResponse> result = await handler.HandleAsync(new ApplyModerationVerdictRequest(Guid.NewGuid(), ModerationVerdict.Approved));

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }
}
