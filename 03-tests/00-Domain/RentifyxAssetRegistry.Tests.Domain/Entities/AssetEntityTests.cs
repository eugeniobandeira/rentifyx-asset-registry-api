using FluentAssertions;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Enums;
using RentifyxAssetRegistry.Domain.Events.Asset;
using RentifyxAssetRegistry.Domain.ValueObjects;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Domain.Entities;

public class AssetEntityTests
{
    private static AssetEntity CreateDraftAsset()
    {
        AssetTitle title = AssetTitle.Create("Excavator CAT 320");
        AssetDescription description = AssetDescription.Create("Heavy duty excavator available for rent.");

        return AssetEntity.Create(Guid.NewGuid(), title, description, Guid.NewGuid(), Guid.NewGuid().ToString());
    }

    [Fact]
    public void Create_ValidData_ReturnsDraftAssetAndRaisesAssetCreated()
    {
        Guid ownerId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        AssetTitle title = AssetTitle.Create("Excavator CAT 320");
        AssetDescription description = AssetDescription.Create("Heavy duty excavator available for rent.");

        string idempotencyKey = Guid.NewGuid().ToString();

        AssetEntity asset = AssetEntity.Create(ownerId, title, description, categoryId, idempotencyKey);

        asset.Status.Should().Be(AssetStatus.Draft);
        asset.OwnerId.Should().Be(ownerId);
        asset.CategoryId.Should().Be(categoryId);
        asset.Title.Should().Be(title);
        asset.Description.Should().Be(description);
        asset.IdempotencyKey.Should().Be(idempotencyKey);
        asset.UpdatedAt.Should().BeNull();
        asset.Id.Should().NotBeEmpty();

        asset.DomainEvents.Should().ContainSingle();
        AssetCreated domainEvent = asset.DomainEvents.Single().Should().BeOfType<AssetCreated>().Subject;
        domainEvent.AssetId.Should().Be(asset.Id);
        domainEvent.OwnerId.Should().Be(ownerId);
        domainEvent.CategoryId.Should().Be(categoryId);
    }

    [Fact]
    public void Create_NullOrWhitespaceIdempotencyKey_Throws()
    {
        AssetTitle title = AssetTitle.Create("Excavator CAT 320");
        AssetDescription description = AssetDescription.Create("Heavy duty excavator available for rent.");

        Action act = () => AssetEntity.Create(Guid.NewGuid(), title, description, Guid.NewGuid(), " ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AttachMedia_ValidMedia_RaisesAssetMediaUploaded()
    {
        AssetEntity asset = CreateDraftAsset();
        asset.ClearDomainEvents();
        Media media = Media.Create("assets/photo.jpg", "image/jpeg", 1024, MediaUploadStatus.Uploaded);

        asset.AttachMedia(media);

        asset.DomainEvents.Should().ContainSingle();
        AssetMediaUploaded domainEvent = asset.DomainEvents.Single().Should().BeOfType<AssetMediaUploaded>().Subject;
        domainEvent.AssetId.Should().Be(asset.Id);
        domainEvent.S3Key.Should().Be(media.S3Key);
    }

    [Fact]
    public void FullLifecycle_HappyPath_TransitionsThroughAllStatuses()
    {
        AssetEntity asset = CreateDraftAsset();
        asset.Status.Should().Be(AssetStatus.Draft);

        asset.SubmitForModeration();
        asset.Status.Should().Be(AssetStatus.PendingModeration);

        asset.Publish();
        asset.Status.Should().Be(AssetStatus.Active);

        asset.Suspend("Reported by user", Guid.NewGuid());
        asset.Status.Should().Be(AssetStatus.Suspended);

        asset.Reinstate();
        asset.Status.Should().Be(AssetStatus.Active);

        asset.Archive();
        asset.Status.Should().Be(AssetStatus.Archived);
    }

    [Fact]
    public void Publish_FromDraft_ThrowsInvalidOperationException()
    {
        AssetEntity asset = CreateDraftAsset();

        Action act = asset.Publish;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void SubmitForModeration_FromNonDraftStatus_ThrowsInvalidOperationException()
    {
        AssetEntity asset = CreateDraftAsset();
        asset.SubmitForModeration();

        Action act = asset.SubmitForModeration;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Suspend_FromNonActiveStatus_ThrowsInvalidOperationException()
    {
        AssetEntity asset = CreateDraftAsset();

        Action act = () => asset.Suspend("Some reason", Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reinstate_FromNonSuspendedStatus_ThrowsInvalidOperationException()
    {
        AssetEntity asset = CreateDraftAsset();

        Action act = asset.Reinstate;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_ThrowsInvalidOperationException()
    {
        AssetEntity asset = CreateDraftAsset();
        asset.Archive();

        Action act = asset.Archive;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Publish_FromPendingModeration_RaisesAssetPublishedWithCorrectPayload()
    {
        AssetEntity asset = CreateDraftAsset();
        asset.SubmitForModeration();
        asset.ClearDomainEvents();

        asset.Publish();

        asset.DomainEvents.Should().ContainSingle();
        AssetPublished domainEvent = asset.DomainEvents.Single().Should().BeOfType<AssetPublished>().Subject;
        domainEvent.AssetId.Should().Be(asset.Id);
    }

    [Fact]
    public void Suspend_FromActive_RaisesAssetSuspendedWithCorrectPayload()
    {
        AssetEntity asset = CreateDraftAsset();
        asset.SubmitForModeration();
        asset.Publish();
        asset.ClearDomainEvents();
        Guid suspendedBy = Guid.NewGuid();

        asset.Suspend("Policy violation", suspendedBy);

        asset.DomainEvents.Should().ContainSingle();
        AssetSuspended domainEvent = asset.DomainEvents.Single().Should().BeOfType<AssetSuspended>().Subject;
        domainEvent.AssetId.Should().Be(asset.Id);
        domainEvent.Reason.Should().Be("Policy violation");
        domainEvent.SuspendedBy.Should().Be(suspendedBy);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Suspend_NullOrWhitespaceReason_ThrowsArgumentException(string? reason)
    {
        AssetEntity asset = CreateDraftAsset();
        asset.SubmitForModeration();
        asset.Publish();

        Action act = () => asset.Suspend(reason!, Guid.NewGuid());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SubmitForModeration_DoesNotRaiseDomainEvent()
    {
        AssetEntity asset = CreateDraftAsset();
        asset.ClearDomainEvents();

        asset.SubmitForModeration();

        asset.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Reinstate_DoesNotRaiseDomainEvent()
    {
        AssetEntity asset = CreateDraftAsset();
        asset.SubmitForModeration();
        asset.Publish();
        asset.Suspend("Reason", Guid.NewGuid());
        asset.ClearDomainEvents();

        asset.Reinstate();

        asset.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Archive_DoesNotRaiseDomainEvent()
    {
        AssetEntity asset = CreateDraftAsset();
        asset.ClearDomainEvents();

        asset.Archive();

        asset.DomainEvents.Should().BeEmpty();
    }
}
