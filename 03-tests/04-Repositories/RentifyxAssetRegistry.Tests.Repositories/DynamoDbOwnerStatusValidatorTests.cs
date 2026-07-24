using FluentAssertions;
using RentifyxAssetRegistry.Infrastructure.Persistence;
using RentifyxAssetRegistry.Tests.Repositories.Fixtures;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Repositories;

[Collection(LocalStackFixtureGroup.Name)]
public sealed class DynamoDbOwnerStatusValidatorTests(LocalStackFixture fixture)
{
    private readonly DynamoDbOwnerStatusValidator _validator = new(fixture.Context, fixture.Options);

    [Fact]
    public async Task IsOwnerActiveAsync_NoCacheEntry_ReturnsFalse()
    {
        bool isActive = await _validator.IsOwnerActiveAsync(Guid.NewGuid());

        isActive.Should().BeFalse();
    }

    [Fact]
    public async Task IsOwnerActiveAsync_CacheEntryIsActiveFalse_ReturnsFalse()
    {
        Guid ownerId = Guid.NewGuid();

        await _validator.UpsertAsync(ownerId, isActive: false, "Suspended", DateTimeOffset.UtcNow);

        bool isActive = await _validator.IsOwnerActiveAsync(ownerId);

        isActive.Should().BeFalse();
    }

    [Fact]
    public async Task IsOwnerActiveAsync_CacheEntryIsActiveTrue_ReturnsTrue()
    {
        Guid ownerId = Guid.NewGuid();

        await _validator.UpsertAsync(ownerId, isActive: true, "Reactivated", DateTimeOffset.UtcNow);

        bool isActive = await _validator.IsOwnerActiveAsync(ownerId);

        isActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpsertAsync_Called_RoundTripsReasonAndUpdatedAt()
    {
        Guid ownerId = Guid.NewGuid();
        DateTimeOffset updatedAt = DateTimeOffset.UtcNow;

        await _validator.UpsertAsync(ownerId, isActive: false, "Deleted", updatedAt);

        bool isActive = await _validator.IsOwnerActiveAsync(ownerId);

        isActive.Should().BeFalse();
    }
}
