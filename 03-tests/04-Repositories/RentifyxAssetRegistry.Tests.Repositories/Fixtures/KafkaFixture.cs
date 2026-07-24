using Testcontainers.Kafka;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Repositories.Fixtures;

/// <summary>
/// Spins up a real Kafka broker once per test collection via Testcontainers.Kafka. See
/// design.md's SPEC_DEVIATION note: the brief only named Testcontainers.LocalStack, but
/// OutboxPublisherTests needs a real broker to assert on.
/// </summary>
public sealed class KafkaFixture : IAsyncLifetime
{
    private KafkaContainer _container = null!;

    public string BootstrapServers { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _container = new KafkaBuilder("apache/kafka:3.7.0").Build();

        await _container.StartAsync();

        BootstrapServers = _container.GetBootstrapAddress();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class OutboxFixtureGroup : ICollectionFixture<LocalStackFixture>, ICollectionFixture<KafkaFixture>
{
    public const string Name = "Outbox";
}

// Separate collection from OutboxFixtureGroup: F-12's consumer tests seed AssetEntity via the real
// DynamoDbAssetRepository.SaveAsync, which writes a pending Outbox entry as a side effect. Sharing
// LocalStackFixture with OutboxPublisherTests let that leftover entry get picked up and published
// by OutboxPublisherTests' own OutboxPublisher instance, corrupting its message-key assertion.
[CollectionDefinition(Name)]
public sealed class CrossServiceFixtureGroup : ICollectionFixture<LocalStackFixture>, ICollectionFixture<KafkaFixture>
{
    public const string Name = "CrossService";
}
