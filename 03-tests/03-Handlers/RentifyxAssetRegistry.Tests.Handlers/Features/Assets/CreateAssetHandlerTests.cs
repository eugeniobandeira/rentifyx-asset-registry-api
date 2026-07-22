using ErrorOr;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create.Validator;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Enums;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;
using RentifyxAssetRegistry.Domain.ValueObjects;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Handlers.Features.Assets;

public sealed class CreateAssetHandlerTests
{
    private static CreateAssetRequest ValidRequest() => new(
        Guid.NewGuid(),
        "Excavator CAT 320",
        "Heavy duty excavator available for rent.",
        1000m,
        Guid.NewGuid(),
        Guid.NewGuid().ToString());

    [Fact]
    public async Task HandleAsync_ValidRequestActiveOwner_CreatesAndSavesAsset()
    {
        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetEntity?)null);

        Mock<IOwnerStatusValidator> ownerStatusValidator = new();
        ownerStatusValidator.Setup(v => v.IsOwnerActiveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        CreateAssetHandler handler = new(
            repository.Object,
            ownerStatusValidator.Object,
            new CreateAssetValidator(),
            NullLogger<CreateAssetHandler>.Instance);

        ErrorOr<CreateAssetResponse> result = await handler.HandleAsync(ValidRequest());

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(AssetStatus.Draft);
        repository.Verify(r => r.SaveAsync(It.IsAny<AssetEntity>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ExistingIdempotencyKey_ReturnsExistingAssetWithoutSavingAgain()
    {
        CreateAssetRequest request = ValidRequest();
        AssetEntity existing = AssetEntity.Create(
            request.OwnerId,
            AssetTitle.Create(request.Title),
            AssetDescription.Create(request.Description),
            Money.Create(request.Price),
            request.CategoryId,
            request.IdempotencyKey);

        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdempotencyKeyAsync(request.IdempotencyKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        Mock<IOwnerStatusValidator> ownerStatusValidator = new();

        CreateAssetHandler handler = new(
            repository.Object,
            ownerStatusValidator.Object,
            new CreateAssetValidator(),
            NullLogger<CreateAssetHandler>.Instance);

        ErrorOr<CreateAssetResponse> result = await handler.HandleAsync(request);

        result.IsError.Should().BeFalse();
        result.Value.AssetId.Should().Be(existing.Id);
        repository.Verify(r => r.SaveAsync(It.IsAny<AssetEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        ownerStatusValidator.Verify(v => v.IsOwnerActiveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_OwnerNotActive_ReturnsForbiddenAndDoesNotSave()
    {
        Mock<IAssetRepository> repository = new();
        repository.Setup(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetEntity?)null);

        Mock<IOwnerStatusValidator> ownerStatusValidator = new();
        ownerStatusValidator.Setup(v => v.IsOwnerActiveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        CreateAssetHandler handler = new(
            repository.Object,
            ownerStatusValidator.Object,
            new CreateAssetValidator(),
            NullLogger<CreateAssetHandler>.Instance);

        ErrorOr<CreateAssetResponse> result = await handler.HandleAsync(ValidRequest());

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Forbidden);
        repository.Verify(r => r.SaveAsync(It.IsAny<AssetEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_InvalidRequest_ReturnsValidationErrorsWithoutTouchingRepository()
    {
        CreateAssetRequest invalidRequest = new(Guid.Empty, "", "", -1m, Guid.Empty, "");

        Mock<IAssetRepository> repository = new();
        Mock<IOwnerStatusValidator> ownerStatusValidator = new();

        CreateAssetHandler handler = new(
            repository.Object,
            ownerStatusValidator.Object,
            new CreateAssetValidator(),
            NullLogger<CreateAssetHandler>.Instance);

        ErrorOr<CreateAssetResponse> result = await handler.HandleAsync(invalidRequest);

        result.IsError.Should().BeTrue();
        repository.Verify(r => r.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(r => r.SaveAsync(It.IsAny<AssetEntity>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
