using System.Text.Json;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RentifyxAssetRegistry.Api.Messaging;
using RentifyxAssetRegistry.Api.Messaging.Contracts;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ApplyModerationVerdict;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ApplyModerationVerdict.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ApplyModerationVerdict.Validator;
using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Enums;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;
using RentifyxAssetRegistry.Domain.ValueObjects;
using RentifyxAssetRegistry.Infrastructure.Persistence;
using RentifyxAssetRegistry.Tests.Repositories.Fixtures;
using FluentValidation;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Repositories;

/// <summary>
/// Drives ModerationVerdictConsumer against a real Kafka broker and LocalStack DynamoDB, calling
/// the real F-09 ApplyModerationVerdictHandler end-to-end. See
/// .specs/features/e04-f12-cross-service-integration/design.md.
/// </summary>
[Collection(OutboxFixtureGroup.Name)]
public sealed class ModerationVerdictConsumerTests(LocalStackFixture dynamoFixture, KafkaFixture kafkaFixture)
{
    [Fact]
    public async Task ExecuteAsync_ApprovedVerdictForPendingModerationAsset_TransitionsAssetToActive()
    {
        DynamoDbAssetRepository repository = new(dynamoFixture.Client, dynamoFixture.Context, dynamoFixture.Options);
        AssetEntity asset = BuildPendingModerationAsset();
        await repository.SaveAsync(asset);

        AssetMediaModeratedEvent evt = new(
            asset.Id,
            ModerationVerdict.Approved,
            [],
            0.1f,
            DateTimeOffset.UtcNow,
            "moderation-bucket",
            $"assets/{asset.OwnerId}/{asset.Id}/media.jpg",
            SchemaVersion: 2);

        await PublishAndConsumeAsync(evt);

        AssetEntity? reloaded = await repository.GetByIdAsync(asset.Id);
        reloaded.Should().NotBeNull();
        reloaded!.Status.Should().Be(AssetStatus.Active);
    }

    [Fact]
    public async Task ExecuteAsync_SchemaVersionMismatch_SkipsWithoutCallingHandler()
    {
        DynamoDbAssetRepository repository = new(dynamoFixture.Client, dynamoFixture.Context, dynamoFixture.Options);
        AssetEntity asset = BuildPendingModerationAsset();
        await repository.SaveAsync(asset);

        AssetMediaModeratedEvent evt = new(
            asset.Id,
            ModerationVerdict.Approved,
            [],
            0.1f,
            DateTimeOffset.UtcNow,
            "moderation-bucket",
            $"assets/{asset.OwnerId}/{asset.Id}/media.jpg",
            SchemaVersion: 1);

        await PublishAndConsumeAsync(evt);

        AssetEntity? reloaded = await repository.GetByIdAsync(asset.Id);
        reloaded.Should().NotBeNull();
        reloaded!.Status.Should().Be(AssetStatus.PendingModeration);
    }

    private static AssetEntity BuildPendingModerationAsset()
    {
        AssetEntity asset = AssetEntity.Create(
            Guid.NewGuid(),
            AssetTitle.Create("Excavator CAT 320"),
            AssetDescription.Create("Heavy duty excavator available for rent."),
            Money.Create(1000m),
            Guid.NewGuid(),
            Guid.NewGuid().ToString());
        asset.SubmitForModeration();

        return asset;
    }

    private async Task PublishAndConsumeAsync(AssetMediaModeratedEvent evt)
    {
        string groupId = $"test-moderation-verdict-{Guid.NewGuid():N}";

        ProducerConfig producerConfig = new() { BootstrapServers = kafkaFixture.BootstrapServers };
        using (IProducer<string, string> producer = new ProducerBuilder<string, string>(producerConfig).Build())
        {
            string payload = JsonSerializer.Serialize(evt);
            await producer.ProduceAsync(
                KafkaTopics.AssetMediaModerated,
                new Message<string, string> { Key = evt.AssetId.ToString(), Value = payload });
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

        using ModerationVerdictConsumer sut = new(consumer, provider.GetRequiredService<IServiceScopeFactory>(), NullLogger<ModerationVerdictConsumer>.Instance);

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(15));
        await sut.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromSeconds(3), CancellationToken.None);
        await sut.StopAsync(CancellationToken.None);
    }

    private ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();
        services.AddSingleton(dynamoFixture.Client);
        services.AddSingleton(dynamoFixture.Context);
        services.AddSingleton(dynamoFixture.Options);
        services.AddScoped<IAssetRepository, DynamoDbAssetRepository>();
        services.AddScoped<IValidator<ApplyModerationVerdictRequest>, ApplyModerationVerdictValidator>();
        services.AddScoped<IHandler<ApplyModerationVerdictRequest, AssetModerationResponse>, ApplyModerationVerdictHandler>();
        services.AddLogging();

        return services.BuildServiceProvider();
    }
}
