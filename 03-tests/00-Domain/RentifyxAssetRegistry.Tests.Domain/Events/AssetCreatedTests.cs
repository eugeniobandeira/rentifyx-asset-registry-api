using FluentAssertions;
using RentifyxAssetRegistry.Domain.Common;
using RentifyxAssetRegistry.Domain.Events.Asset;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Domain.Events;

public sealed class AssetCreatedTests
{
    [Fact]
    public void Constructor_ValidValues_ShouldRoundTripProperties()
    {
        Guid assetId = Guid.NewGuid();
        Guid ownerId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        DateTime occurredAt = DateTime.UtcNow;

        AssetCreated domainEvent = new(assetId, ownerId, categoryId, occurredAt);

        domainEvent.AssetId.Should().Be(assetId);
        domainEvent.OwnerId.Should().Be(ownerId);
        domainEvent.CategoryId.Should().Be(categoryId);
        domainEvent.OccurredAt.Should().Be(occurredAt);
    }

    [Fact]
    public void Constructor_ValidValues_ShouldImplementIDomainEvent()
    {
        AssetCreated domainEvent = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow);

        domainEvent.Should().BeAssignableTo<IDomainEvent>();
    }
}
