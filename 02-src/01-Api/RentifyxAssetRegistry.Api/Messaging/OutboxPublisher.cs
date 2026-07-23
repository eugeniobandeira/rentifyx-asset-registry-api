using System.Globalization;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Domain.Events.Asset;
using RentifyxAssetRegistry.Infrastructure.Configuration;
using RentifyxAssetRegistry.Infrastructure.Persistence;
using RentifyxAssetRegistry.Infrastructure.Persistence.Items;
using RentifyxAssetRegistry.Infrastructure.Persistence.Mappers;

namespace RentifyxAssetRegistry.Api.Messaging;

/// <summary>
/// Polls the DynamoDB outbox (GSI1 <c>OUTBOX_STATUS#Pending</c> partition) on a fixed interval and
/// publishes each pending entry's already-serialized domain event payload to Kafka, keyed by
/// AssetId to preserve per-asset ordering within a partition. See
/// .specs/features/e04-f10-dynamodb-repository/design.md's Outbox publisher section.
/// </summary>
public sealed class OutboxPublisher(
    IAmazonDynamoDB client,
    DynamoDbOptions dynamoDbOptions,
    IProducer<string, string> producer,
    IOptions<OutboxOptions> options,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    private const string StatusAttribute = "Status";
    private const string RetryCountAttribute = "RetryCount";
    private const string AssetIdPayloadProperty = "AssetId";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        OutboxOptions outboxOptions = options.Value;
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(outboxOptions.PollIntervalSeconds));

        do
        {
            try
            {
                await PublishPendingBatchAsync(outboxOptions, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox publish cycle failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        producer.Flush(TimeSpan.FromSeconds(10));
    }

    private async Task PublishPendingBatchAsync(OutboxOptions outboxOptions, CancellationToken ct)
    {
        QueryRequest request = new()
        {
            TableName = dynamoDbOptions.TableName,
            IndexName = DynamoDbKeys.Gsi1IndexName,
            KeyConditionExpression = "#gsi1pk = :pk",
            ExpressionAttributeNames = new Dictionary<string, string> { ["#gsi1pk"] = DynamoDbKeys.Gsi1Pk },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new AttributeValue(DynamoDbKeys.OutboxStatusKey(OutboxDynamoDbMapper.OutboxStatusPending))
            },
            Limit = outboxOptions.BatchSize
        };

        QueryResponse response = await client.QueryAsync(request, ct);

        if (response.Items.Count == 0)
            return;

        foreach (Dictionary<string, AttributeValue> rawItem in response.Items)
        {
            OutboxItem item = OutboxDynamoDbMapper.FromAttributeMap(rawItem);
            await PublishEntryAsync(item, outboxOptions, ct);
        }
    }

    private async Task PublishEntryAsync(OutboxItem item, OutboxOptions outboxOptions, CancellationToken ct)
    {
        try
        {
            string topic = ResolveTopic(item.EventType);
            string key = ExtractAssetId(item.Payload) ?? item.Id;

            await producer.ProduceAsync(topic, new Message<string, string> { Key = key, Value = item.Payload }, ct);

            await MarkPublishedAsync(item, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await HandleFailureAsync(item, outboxOptions, ex, ct);
        }
    }

    private async Task MarkPublishedAsync(OutboxItem item, CancellationToken ct)
    {
        await client.UpdateItemAsync(
            new UpdateItemRequest
            {
                TableName = dynamoDbOptions.TableName,
                Key = KeyFor(item),
                UpdateExpression = "SET #status = :status REMOVE #gsi1pk, #gsi1sk",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#status"] = StatusAttribute,
                    ["#gsi1pk"] = DynamoDbKeys.Gsi1Pk,
                    ["#gsi1sk"] = DynamoDbKeys.Gsi1Sk
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":status"] = new AttributeValue(OutboxDynamoDbMapper.OutboxStatusPublished)
                }
            },
            ct);
    }

    private async Task HandleFailureAsync(OutboxItem item, OutboxOptions outboxOptions, Exception ex, CancellationToken ct)
    {
        int retryCount = item.RetryCount + 1;

        if (retryCount < outboxOptions.MaxRetries)
        {
            await client.UpdateItemAsync(
                new UpdateItemRequest
                {
                    TableName = dynamoDbOptions.TableName,
                    Key = KeyFor(item),
                    UpdateExpression = $"SET {RetryCountAttribute} = :retryCount",
                    ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                    {
                        [":retryCount"] = new AttributeValue { N = retryCount.ToString(CultureInfo.InvariantCulture) }
                    }
                },
                ct);

            logger.LogWarning(
                ex,
                "Outbox entry {OutboxId} publish failed (attempt {RetryCount}/{MaxRetries}); will retry.",
                item.Id,
                retryCount,
                outboxOptions.MaxRetries);

            return;
        }

        await client.UpdateItemAsync(
            new UpdateItemRequest
            {
                TableName = dynamoDbOptions.TableName,
                Key = KeyFor(item),
                UpdateExpression = $"SET #status = :status, {RetryCountAttribute} = :retryCount REMOVE #gsi1pk, #gsi1sk",
                ExpressionAttributeNames = new Dictionary<string, string>
                {
                    ["#status"] = StatusAttribute,
                    ["#gsi1pk"] = DynamoDbKeys.Gsi1Pk,
                    ["#gsi1sk"] = DynamoDbKeys.Gsi1Sk
                },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":status"] = new AttributeValue(OutboxDynamoDbMapper.OutboxStatusFailed),
                    [":retryCount"] = new AttributeValue { N = retryCount.ToString(CultureInfo.InvariantCulture) }
                }
            },
            ct);

        logger.LogCritical(
            ex,
            "Outbox entry {OutboxId} exceeded max retries ({MaxRetries}) and is marked Failed.",
            item.Id,
            outboxOptions.MaxRetries);
    }

    private static Dictionary<string, AttributeValue> KeyFor(OutboxItem item) => new()
    {
        [DynamoDbKeys.Pk] = new AttributeValue(item.Pk),
        [DynamoDbKeys.Sk] = new AttributeValue(item.Sk)
    };

    private static string ResolveTopic(string eventType) => eventType switch
    {
        nameof(AssetCreated) => KafkaTopics.AssetCreated,
        nameof(AssetMediaUploaded) => KafkaTopics.AssetMediaUploaded,
        nameof(AssetPublished) => KafkaTopics.AssetPublished,
        nameof(AssetSuspended) => KafkaTopics.AssetSuspended,
        _ => throw new InvalidOperationException($"Unknown outbox event type '{eventType}'.")
    };

    private static string? ExtractAssetId(string payload)
    {
        using JsonDocument document = JsonDocument.Parse(payload);

        return document.RootElement.TryGetProperty(AssetIdPayloadProperty, out JsonElement value)
            ? value.GetString()
            : null;
    }
}
