using ErrorOr;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.RequestMediaUpload;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.RequestMediaUpload.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.RequestMediaUpload.Validator;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;
using RentifyxAssetRegistry.Domain.Interfaces.Media;
using RentifyxAssetRegistry.Domain.ValueObjects;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Handlers.Features.Assets;

public sealed class RequestMediaUploadHandlerTests
{
    private static AssetEntity CreateAsset(Guid ownerId) => AssetEntity.Create(
        ownerId,
        AssetTitle.Create("Excavator CAT 320"),
        AssetDescription.Create("Heavy duty excavator available for rent."),
        Money.Create(1000m),
        Guid.NewGuid(),
        Guid.NewGuid().ToString());

    [Fact]
    public async Task HandleAsync_ValidRequest_ReturnsPresignedUrlWithoutTouchingSave()
    {
        Guid ownerId = Guid.NewGuid();
        AssetEntity asset = CreateAsset(ownerId);

        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMediaStorageService> mediaStorageService = new();
        mediaStorageService
            .Setup(m => m.GeneratePresignedUploadUrlAsync(ownerId, asset.Id, "image/jpeg", 1024, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PresignedUploadUrl("https://s3.example.com/upload", $"assets/{ownerId}/{asset.Id}/photo.jpg"));

        RequestMediaUploadHandler handler = new(
            repository.Object,
            mediaStorageService.Object,
            new RequestMediaUploadValidator(),
            NullLogger<RequestMediaUploadHandler>.Instance);

        ErrorOr<RequestMediaUploadResponse> result = await handler.HandleAsync(
            new RequestMediaUploadRequest(asset.Id, ownerId, "image/jpeg", 1024));

        result.IsError.Should().BeFalse();
        result.Value.UploadUrl.Should().Be("https://s3.example.com/upload");
        repository.Verify(r => r.SaveAsync(It.IsAny<AssetEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_AssetNotFound_ReturnsNotFoundWithoutCallingStorage()
    {
        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetEntity?)null);

        Mock<IMediaStorageService> mediaStorageService = new();

        RequestMediaUploadHandler handler = new(
            repository.Object,
            mediaStorageService.Object,
            new RequestMediaUploadValidator(),
            NullLogger<RequestMediaUploadHandler>.Instance);

        ErrorOr<RequestMediaUploadResponse> result = await handler.HandleAsync(
            new RequestMediaUploadRequest(Guid.NewGuid(), Guid.NewGuid(), "image/jpeg", 1024));

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
        mediaStorageService.Verify(
            m => m.GeneratePresignedUploadUrlAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_OwnerMismatch_ReturnsForbiddenWithoutCallingStorage()
    {
        AssetEntity asset = CreateAsset(Guid.NewGuid());

        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdAsync(asset.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);

        Mock<IMediaStorageService> mediaStorageService = new();

        RequestMediaUploadHandler handler = new(
            repository.Object,
            mediaStorageService.Object,
            new RequestMediaUploadValidator(),
            NullLogger<RequestMediaUploadHandler>.Instance);

        ErrorOr<RequestMediaUploadResponse> result = await handler.HandleAsync(
            new RequestMediaUploadRequest(asset.Id, Guid.NewGuid(), "image/jpeg", 1024));

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Forbidden);
        mediaStorageService.Verify(
            m => m.GeneratePresignedUploadUrlAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_OversizedFile_ReturnsValidationErrorWithoutCallingStorage()
    {
        Mock<IAssetRepository> repository = new();
        Mock<IMediaStorageService> mediaStorageService = new();

        RequestMediaUploadHandler handler = new(
            repository.Object,
            mediaStorageService.Object,
            new RequestMediaUploadValidator(),
            NullLogger<RequestMediaUploadHandler>.Instance);

        ErrorOr<RequestMediaUploadResponse> result = await handler.HandleAsync(
            new RequestMediaUploadRequest(Guid.NewGuid(), Guid.NewGuid(), "image/jpeg", 0));

        result.IsError.Should().BeTrue();
        repository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        mediaStorageService.Verify(
            m => m.GeneratePresignedUploadUrlAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_DisallowedMimeType_ReturnsValidationErrorWithoutCallingStorage()
    {
        Mock<IAssetRepository> repository = new();
        Mock<IMediaStorageService> mediaStorageService = new();

        RequestMediaUploadHandler handler = new(
            repository.Object,
            mediaStorageService.Object,
            new RequestMediaUploadValidator(),
            NullLogger<RequestMediaUploadHandler>.Instance);

        ErrorOr<RequestMediaUploadResponse> result = await handler.HandleAsync(
            new RequestMediaUploadRequest(Guid.NewGuid(), Guid.NewGuid(), "application/exe", 1024));

        result.IsError.Should().BeTrue();
        mediaStorageService.Verify(
            m => m.GeneratePresignedUploadUrlAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
