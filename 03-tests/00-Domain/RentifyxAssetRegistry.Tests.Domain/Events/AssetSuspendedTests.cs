using FluentAssertions;
using RentifyxAssetRegistry.Domain.Common;
using RentifyxAssetRegistry.Domain.Events.Asset;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Domain.Events;

public class AssetSuspendedTests
{
    [Fact]
    public void Constructor_ValidValues_ShouldRoundTripProperties()
    {
        Guid assetId = Guid.NewGuid();
        string reason = "Policy violation";
        Guid suspendedBy = Guid.NewGuid();
        DateTime occurredAt = DateTime.UtcNow;

        AssetSuspended domainEvent = new(assetId, reason, suspendedBy, occurredAt);

        domainEvent.AssetId.Should().Be(assetId);
        domainEvent.Reason.Should().Be(reason);
        domainEvent.SuspendedBy.Should().Be(suspendedBy);
        domainEvent.OccurredAt.Should().Be(occurredAt);
        domainEvent.Should().BeAssignableTo<IDomainEvent>();
    }
}
