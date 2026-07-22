using RentifyxAssetRegistry.Domain.Common;
using RentifyxAssetRegistry.Domain.Enums;
using RentifyxAssetRegistry.Domain.Events.Asset;
using RentifyxAssetRegistry.Domain.ValueObjects;

namespace RentifyxAssetRegistry.Domain.Entities;

public sealed class AssetEntity : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public AssetTitle Title { get; private set; } = null!;
    public AssetDescription Description { get; private set; } = null!;
    public Guid CategoryId { get; private set; }
    public AssetStatus Status { get; private set; }
    public string IdempotencyKey { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private AssetEntity()
    {
    }

    public static AssetEntity Create(
        Guid ownerId,
        AssetTitle title,
        AssetDescription description,
        Guid categoryId,
        string idempotencyKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        DateTime occurredAt = DateTime.UtcNow;

        AssetEntity asset = new()
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Title = title,
            Description = description,
            CategoryId = categoryId,
            Status = AssetStatus.Draft,
            IdempotencyKey = idempotencyKey,
            CreatedAt = occurredAt,
            UpdatedAt = null
        };

        asset.RaiseDomainEvent(new AssetCreated(asset.Id, ownerId, categoryId, occurredAt));

        return asset;
    }

    public void AttachMedia(Media media)
    {
        RaiseDomainEvent(new AssetMediaUploaded(Id, media.S3Key, DateTime.UtcNow));
    }

    public void SubmitForModeration()
    {
        if (Status != AssetStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Cannot submit for moderation from status '{Status}'. Asset must be in 'Draft' status.");
        }

        Status = AssetStatus.PendingModeration;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Publish()
    {
        if (Status != AssetStatus.PendingModeration)
        {
            throw new InvalidOperationException(
                $"Cannot publish from status '{Status}'. Asset must be in 'PendingModeration' status.");
        }

        Status = AssetStatus.Active;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new AssetPublished(Id, UpdatedAt.Value));
    }

    public void Suspend(string reason, Guid suspendedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (Status != AssetStatus.Active)
        {
            throw new InvalidOperationException(
                $"Cannot suspend from status '{Status}'. Asset must be in 'Active' status.");
        }

        Status = AssetStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new AssetSuspended(Id, reason, suspendedBy, UpdatedAt.Value));
    }

    public void Reinstate()
    {
        if (Status != AssetStatus.Suspended)
        {
            throw new InvalidOperationException(
                $"Cannot reinstate from status '{Status}'. Asset must be in 'Suspended' status.");
        }

        Status = AssetStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        if (Status == AssetStatus.Archived)
        {
            throw new InvalidOperationException("Asset is already archived.");
        }

        Status = AssetStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }
}
