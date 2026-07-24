using System.Text.Json;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RentifyxAssetRegistry.Api.Messaging;
using RentifyxAssetRegistry.Api.Messaging.Contracts;
using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Infrastructure.Persistence;
using RentifyxAssetRegistry.Tests.Repositories.Fixtures;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Repositories;

/// <summary>
/// Drives OwnerStatusConsumer against a real Kafka broker and LocalStack DynamoDB. See
/// .specs/features/e04-f12-cross-service-integration/design.md.
/// </summary>
[Collection(CrossServiceFixtureGroup.Name)]
public sealed class OwnerStatusConsumerTests(LocalStackFixture dynamoFixture, KafkaFixture kafkaFixture)
{
    [Fact]
    public async Task ExecuteAsync_UserSuspendedMessage_UpsertsOwnerStatusCacheAsInactive()
    {
        Guid ownerId = Guid.NewGuid();
        UserLifecycleEventEnvelope envelope = new(
            "UserSuspended",
            ownerId,
            DateTimeOffset.UtcNow,
            JsonSerializer.SerializeToElement(new UserSuspendedPayload(ownerId, "Fraud", DateTimeOffset.UtcNow)));

        await RunConsumerAgainstMessageAsync(envelope);

        DynamoDbOwnerStatusValidator validator = new(dynamoFixture.Context, dynamoFixture.Options);
        bool isActive = await validator.IsOwnerActiveAsync(ownerId);

        isActive.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_UserAccountDeletedMessage_UpsertsOwnerStatusCacheAsInactive()
    {
        Guid ownerId = Guid.NewGuid();
        UserLifecycleEventEnvelope envelope = new(
            "UserAccountDeleted",
            ownerId,
            DateTimeOffset.UtcNow,
            JsonSerializer.SerializeToElement(new UserAccountDeletedPayload(ownerId, DateTimeOffset.UtcNow)));

        await RunConsumerAgainstMessageAsync(envelope);

        DynamoDbOwnerStatusValidator validator = new(dynamoFixture.Context, dynamoFixture.Options);
        bool isActive = await validator.IsOwnerActiveAsync(ownerId);

        isActive.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_MalformedMessage_CommitsOffsetWithoutThrowing()
    {
        string groupId = $"test-owner-status-{Guid.NewGuid():N}";

        ProducerConfig producerConfig = new() { BootstrapServers = kafkaFixture.BootstrapServers };
        using (IProducer<string, string> producer = new ProducerBuilder<string, string>(producerConfig).Build())
        {
            await producer.ProduceAsync(KafkaTopics.UserLifecycleEvents, new Message<string, string> { Key = "bad", Value = "{ not valid json" });
        }

        await using ServiceProvider provider = BuildProvider();

        ConsumerConfig consumerConfig = new()
        {
            BootstrapServers = kafkaFixture.BootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        using IConsumer<string, string> consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();

        using OwnerStatusConsumer sut = new(consumer, provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<OwnerStatusConsumer>.Instance);

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(10));
        Func<Task> act = () => sut.StartAsync(cts.Token);

        await act.Should().NotThrowAsync();
        await Task.Delay(TimeSpan.FromSeconds(2), CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);
    }

    private async Task RunConsumerAgainstMessageAsync(UserLifecycleEventEnvelope envelope)
    {
        string groupId = $"test-owner-status-{Guid.NewGuid():N}";

        ProducerConfig producerConfig = new() { BootstrapServers = kafkaFixture.BootstrapServers };
        using (IProducer<string, string> producer = new ProducerBuilder<string, string>(producerConfig).Build())
        {
            string payload = JsonSerializer.Serialize(envelope);
            await producer.ProduceAsync(
                KafkaTopics.UserLifecycleEvents,
                new Message<string, string> { Key = envelope.AggregateId.ToString(), Value = payload });
        }

        await using ServiceProvider provider = BuildProvider();

        ConsumerConfig consumerConfig = new()
        {
            BootstrapServers = kafkaFixture.BootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        using IConsumer<string, string> consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();

        using OwnerStatusConsumer sut = new(consumer, provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<OwnerStatusConsumer>.Instance);

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(15));
        await sut.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(3), CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);
    }

    private ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();
        services.AddSingleton(dynamoFixture.Context);
        services.AddSingleton(dynamoFixture.Options);
        services.AddScoped<IOwnerStatusCacheWriter, DynamoDbOwnerStatusValidator>();

        return services.BuildServiceProvider();
    }
}
