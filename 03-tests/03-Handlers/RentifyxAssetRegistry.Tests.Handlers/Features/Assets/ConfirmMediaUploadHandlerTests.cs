using ErrorOr;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ConfirmMediaUpload;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ConfirmMediaUpload.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ConfirmMediaUpload.Validator;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Events.Asset;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;
using RentifyxAssetRegistry.Domain.ValueObjects;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Handlers.Features.Assets;

public sealed class ConfirmMediaUploadHandlerTests
{
    private static AssetEntity CreateAsset(Guid ownerId) => AssetEntity.Create(
        ownerId,
        AssetTitle.Create("Excavator CAT 320"),
        AssetDescription.Create("Heavy duty excavator available for rent."),
        Money.Create(1000m),
        Guid.NewGuid(),
        Guid.NewGuid().ToString());

    [Fact]
    public async Task HandleAsync_ValidRequest_AttachesMediaRaisesEventAndSaves()
    {
        Guid ownerId = Guid.NewGuid();
        AssetEntity asset = CreateAsset(ownerId);
        string s3Key = $"assets/{ownerId}/{asset.Id}/photo.jpg";

        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        ConfirmMediaUploadHandler handler = new(
            repository.Object,
            new ConfirmMediaUploadValidator(),
            NullLogger<ConfirmMediaUploadHandler>.Instance);

        ErrorOr<ConfirmMediaUploadResponse> result = await handler.HandleAsync(
            new ConfirmMediaUploadRequest(asset.Id, ownerId, s3Key, "image/jpeg", 1024));

        result.IsError.Should().BeFalse();
        result.Value.S3Key.Should().Be(s3Key);
        asset.DomainEvents.OfType<AssetMediaUploaded>().Should().ContainSingle(e => e.S3Key == s3Key);
        repository.Verify(r => r.SaveAsync(asset, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_AssetNotFound_ReturnsNotFoundWithoutSaving()
    {
        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetEntity?)null);

        ConfirmMediaUploadHandler handler = new(
            repository.Object,
            new ConfirmMediaUploadValidator(),
            NullLogger<ConfirmMediaUploadHandler>.Instance);

        ErrorOr<ConfirmMediaUploadResponse> result = await handler.HandleAsync(
            new ConfirmMediaUploadRequest(Guid.NewGuid(), Guid.NewGuid(), "assets/owner/asset/photo.jpg", "image/jpeg", 1024));

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        repository.Verify(r => r.SaveAsync(It.IsAny<AssetEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_OwnerMismatch_ReturnsForbiddenWithoutSaving()
    {
        AssetEntity asset = CreateAsset(Guid.NewGuid());

        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        ConfirmMediaUploadHandler handler = new(
            repository.Object,
            new ConfirmMediaUploadValidator(),
            NullLogger<ConfirmMediaUploadHandler>.Instance);

        ErrorOr<ConfirmMediaUploadResponse> result = await handler.HandleAsync(
            new ConfirmMediaUploadRequest(asset.Id, Guid.NewGuid(), "assets/owner/asset/photo.jpg", "image/jpeg", 1024));

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Forbidden);
        repository.Verify(r => r.SaveAsync(It.IsAny<AssetEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_DisallowedMimeType_ReturnsValidationErrorWithoutSaving()
    {
        Mock<IAssetRepository> repository = new();

        ConfirmMediaUploadHandler handler = new(
            repository.Object,
            new ConfirmMediaUploadValidator(),
            NullLogger<ConfirmMediaUploadHandler>.Instance);

        ErrorOr<ConfirmMediaUploadResponse> result = await handler.HandleAsync(
            new ConfirmMediaUploadRequest(Guid.NewGuid(), Guid.NewGuid(), "assets/owner/asset/photo.jpg", "application/exe", 1024));

        result.IsError.Should().BeTrue();
        repository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(r => r.SaveAsync(It.IsAny<AssetEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
