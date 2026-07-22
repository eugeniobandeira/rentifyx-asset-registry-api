using FluentAssertions;
using RentifyxAssetRegistry.Domain.Common;
using RentifyxAssetRegistry.Domain.Events.Asset;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Domain.Events;

public sealed class AssetPublishedTests
{
    [Fact]
    public void Constructor_ValidValues_ShouldRoundTripProperties()
    {
        Guid assetId = Guid.NewGuid();
        DateTime occurredAt = DateTime.UtcNow;

        AssetPublished domainEvent = new(assetId, occurredAt);

        domainEvent.AssetId.Should().Be(assetId);
        domainEvent.OccurredAt.Should().Be(occurredAt);
    }

    [Fact]
    public void Constructor_ValidValues_ShouldImplementIDomainEvent()
    {
        AssetPublished domainEvent = new(Guid.NewGuid(), DateTime.UtcNow);

        domainEvent.Should().BeAssignableTo<IDomainEvent>();
    }
}
