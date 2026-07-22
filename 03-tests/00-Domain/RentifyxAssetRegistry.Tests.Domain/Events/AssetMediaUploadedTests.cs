using FluentAssertions;
using RentifyxAssetRegistry.Domain.Common;
using RentifyxAssetRegistry.Domain.Events.Asset;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Domain.Events;

public class AssetMediaUploadedTests
{
    [Fact]
    public void Constructor_ValidValues_ShouldRoundTripProperties()
    {
        Guid assetId = Guid.NewGuid();
        string s3Key = "assets/abc.jpg";
        DateTime occurredAt = DateTime.UtcNow;

        AssetMediaUploaded domainEvent = new(assetId, s3Key, occurredAt);

        domainEvent.AssetId.Should().Be(assetId);
        domainEvent.S3Key.Should().Be(s3Key);
        domainEvent.OccurredAt.Should().Be(occurredAt);
        domainEvent.Should().BeAssignableTo<IDomainEvent>();
    }
}
