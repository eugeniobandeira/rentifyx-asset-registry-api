using Amazon.DynamoDBv2.Model;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RentifyxAssetRegistry.Api.Messaging;
using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Events.Asset;
using RentifyxAssetRegistry.Domain.ValueObjects;
using RentifyxAssetRegistry.Infrastructure.Persistence;
using RentifyxAssetRegistry.Infrastructure.Persistence.Items;
using RentifyxAssetRegistry.Infrastructure.Persistence.Mappers;
using RentifyxAssetRegistry.Tests.Repositories.Fixtures;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Repositories;

/// <summary>
/// Drives one real poll cycle of <see cref="OutboxPublisher"/> against LocalStack DynamoDB and a
/// real Kafka broker (Testcontainers.Kafka - see design.md's SPEC_DEVIATION note).
/// </summary>
[Collection(OutboxFixtureGroup.Name)]
public sealed class OutboxPublisherTests(LocalStackFixture dynamoFixture, KafkaFixture kafkaFixture)
{
    [Fact]
    public async Task ExecuteAsync_PendingOutboxEntry_PublishesToKafkaAndMarksPublished()
    {
        AssetEntity asset = AssetEntity.Create(
            Guid.NewGuid(),
            AssetTitle.Create("Outbox Publisher Test Asset"),
            AssetDescription.Create("A sufficiently long description for validation purposes."),
            Money.Create(150m),
            Guid.NewGuid(),
            Guid.NewGuid().ToString());

        AssetCreated domainEvent = (AssetCreated)asset.DomainEvents.Single();
        OutboxItem outboxItem = OutboxDynamoDbMapper.ToItem(domainEvent);

        await dynamoFixture.Client.PutItemAsync(new PutItemRequest
        {
            TableName = dynamoFixture.Options.TableName,
            Item = OutboxDynamoDbMapper.ToAttributeMap(outboxItem)
        });

        ProducerConfig producerConfig = new() { BootstrapServers = kafkaFixture.BootstrapServers };
        using IProducer<string, string> producer = new ProducerBuilder<string, string>(producerConfig).Build();

        OutboxOptions outboxOptions = new() { PollIntervalSeconds = 1, BatchSize = 25, MaxRetries = 3 };

        using OutboxPublisher publisher = new(
            dynamoFixture.Client,
            dynamoFixture.Options,
            producer,
            Options.Create(outboxOptions),
            NullLogger<OutboxPublisher>.Instance);

        using CancellationTokenSource cts = new(TimeSpan.FromSeconds(20));

        await publisher.StartAsync(cts.Token);

        Dictionary<string, AttributeValue> key = new()
        {
            [DynamoDbKeys.Pk] = new AttributeValue(outboxItem.Pk),
            [DynamoDbKeys.Sk] = new AttributeValue(outboxItem.Sk)
        };

        string status = OutboxDynamoDbMapper.OutboxStatusPending;

        while (status == OutboxDynamoDbMapper.OutboxStatusPending && !cts.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), CancellationToken.None);

            GetItemResponse getResponse = await dynamoFixture.Client.GetItemAsync(new GetItemRequest
            {
                TableName = dynamoFixture.Options.TableName,
                Key = key
            });

            status = getResponse.Item[StatusAttribute()].S;
        }

        await publisher.StopAsync(CancellationToken.None);

        status.Should().Be(OutboxDynamoDbMapper.OutboxStatusPublished);

        ConsumerConfig consumerConfig = new()
        {
            BootstrapServers = kafkaFixture.BootstrapServers,
            GroupId = $"test-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using IConsumer<string, string> consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(KafkaTopics.AssetCreated);

        ConsumeResult<string, string>? result = consumer.Consume(TimeSpan.FromSeconds(30));

        result.Should().NotBeNull();
        result!.Message.Key.Should().Be(asset.Id.ToString());
        result.Message.Value.Should().Contain(asset.Id.ToString());

        consumer.Close();
    }

    private static string StatusAttribute() => "Status";
}
