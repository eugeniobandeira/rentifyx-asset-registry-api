using System.Text.Json;
using Confluent.Kafka;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Api.Messaging.Contracts;
using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Infrastructure.Persistence;

namespace RentifyxAssetRegistry.Api.Messaging;

/// <summary>
/// Malformed/unrecognized messages are poison pills: logged and committed, never retried forever.
/// DynamoDB write failures are NOT committed, letting Kafka redeliver on the next poll.
/// </summary>
public sealed class OwnerStatusConsumer(
    [FromKeyedServices(CrossServiceConsumingExtensions.OwnerStatusConsumerKey)] IConsumer<string, string> consumer,
    IServiceScopeFactory scopeFactory,
    ILogger<OwnerStatusConsumer> logger) : BackgroundService
{
    private const string UserSuspendedEventType = "UserSuspended";
    private const string UserAccountDeletedEventType = "UserAccountDeleted";
    private const string SuspendedReason = "Suspended";
    private const string DeletedReason = "Deleted";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe(KafkaTopics.UserLifecycleEvents);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result;

            try
            {
                result = consumer.Consume(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ConsumeException ex)
            {
                logger.LogError(ex, "Kafka consume error on user-lifecycle-events; retrying.");
                continue;
            }

            if (result?.Message is null)
                continue;

            await ProcessMessageAsync(result, stoppingToken);
        }
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> result, CancellationToken ct)
    {
        UserLifecycleEventEnvelope envelope;

        try
        {
            envelope = JsonSerializer.Deserialize<UserLifecycleEventEnvelope>(result.Message.Value)
                ?? throw new JsonException("Deserialized envelope was null.");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Malformed user-lifecycle-events message at offset {Offset}; skipping.", result.Offset);
            consumer.Commit(result);
            return;
        }

        (Guid userId, string reason, DateTimeOffset occurredAt)? parsed = envelope.EventType switch
        {
            UserSuspendedEventType => ParseSuspended(envelope.Data),
            UserAccountDeletedEventType => ParseDeleted(envelope.Data),
            _ => null
        };

        if (parsed is null)
        {
            logger.LogWarning(
                "Unrecognized EventType '{EventType}' on user-lifecycle-events at offset {Offset}; skipping.",
                envelope.EventType, result.Offset);
            consumer.Commit(result);
            return;
        }

        try
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            IOwnerStatusCacheWriter writer = scope.ServiceProvider.GetRequiredService<IOwnerStatusCacheWriter>();

            await writer.UpsertAsync(parsed.Value.userId, isActive: false, parsed.Value.reason, parsed.Value.occurredAt, ct);

            consumer.Commit(result);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(
                ex,
                "Failed to upsert owner-status cache for OwnerId={OwnerId}; will retry on redelivery.",
                parsed.Value.userId);
        }
    }

    private static (Guid, string, DateTimeOffset) ParseSuspended(JsonElement data)
    {
        UserSuspendedPayload payload = data.Deserialize<UserSuspendedPayload>()
            ?? throw new JsonException("UserSuspended payload was null.");

        return (payload.UserId, SuspendedReason, payload.OccurredAt);
    }

    private static (Guid, string, DateTimeOffset) ParseDeleted(JsonElement data)
    {
        UserAccountDeletedPayload payload = data.Deserialize<UserAccountDeletedPayload>()
            ?? throw new JsonException("UserAccountDeleted payload was null.");

        return (payload.UserId, DeletedReason, payload.OccurredAt);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Close() while another thread is blocked inside Consume() can hang waiting for a clean
        // group-leave the stuck poll loop never yields to, so cancel and let ExecuteAsync's loop
        // exit first (base.StopAsync signals the stoppingToken and awaits it).
        await base.StopAsync(cancellationToken);

        consumer.Close();
    }
}
