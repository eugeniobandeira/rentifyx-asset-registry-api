using System.Text.Json;
using Confluent.Kafka;
using ErrorOr;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Api.Messaging.Contracts;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ApplyModerationVerdict.Request;
using RentifyxAssetRegistry.Domain.Constants;

namespace RentifyxAssetRegistry.Api.Messaging;

/// <summary>
/// The handler's own idempotent-replay behavior (asset not currently PendingModeration) makes
/// redelivered/duplicate messages safe without any consumer-side dedup.
/// </summary>
public sealed class ModerationVerdictConsumer(
    [FromKeyedServices(CrossServiceConsumingExtensions.ModerationVerdictConsumerKey)] IConsumer<string, string> consumer,
    IServiceScopeFactory scopeFactory,
    ILogger<ModerationVerdictConsumer> logger) : BackgroundService
{
    private const int ExpectedSchemaVersion = 2;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe(KafkaTopics.AssetMediaModerated);

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
                logger.LogError(ex, "Kafka consume error on asset-media-moderated; retrying.");
                continue;
            }

            if (result?.Message is null)
                continue;

            await ProcessMessageAsync(result, stoppingToken);
        }
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, string> result, CancellationToken ct)
    {
        AssetMediaModeratedEvent evt;

        try
        {
            evt = JsonSerializer.Deserialize<AssetMediaModeratedEvent>(result.Message.Value)
                ?? throw new JsonException("Deserialized AssetMediaModeratedEvent was null.");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Malformed asset-media-moderated message at offset {Offset}; skipping.", result.Offset);
            consumer.Commit(result);
            return;
        }

        if (evt.SchemaVersion != ExpectedSchemaVersion)
        {
            logger.LogWarning(
                "Unexpected SchemaVersion {SchemaVersion} (expected {Expected}) for AssetId={AssetId}; skipping.",
                evt.SchemaVersion, ExpectedSchemaVersion, evt.AssetId);
            consumer.Commit(result);
            return;
        }

        using IServiceScope scope = scopeFactory.CreateScope();
        IHandler<ApplyModerationVerdictRequest, AssetModerationResponse> handler =
            scope.ServiceProvider.GetRequiredService<IHandler<ApplyModerationVerdictRequest, AssetModerationResponse>>();

        ErrorOr<AssetModerationResponse> handlerResult = await handler.HandleAsync(
            new ApplyModerationVerdictRequest(evt.AssetId, evt.Verdict), ct);

        if (!handlerResult.IsError)
        {
            consumer.Commit(result);
            return;
        }

        if (handlerResult.FirstError.Type == ErrorType.NotFound)
        {
            logger.LogWarning(
                "AssetId={AssetId} not found for moderation verdict; skipping (poison pill).", evt.AssetId);
            consumer.Commit(result);
            return;
        }

        logger.LogError(
            "ApplyModerationVerdictHandler failed for AssetId={AssetId}: {Error}. Will retry on redelivery.",
            evt.AssetId, handlerResult.FirstError.Description);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // See OwnerStatusConsumer.StopAsync: cancel and let the Consume() loop exit before Close().
        await base.StopAsync(cancellationToken);

        consumer.Close();
    }
}
