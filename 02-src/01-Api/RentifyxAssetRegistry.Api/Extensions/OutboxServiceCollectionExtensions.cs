using Confluent.Kafka;
using RentifyxAssetRegistry.Api.Messaging;
using RentifyxAssetRegistry.Infrastructure.Constants;

namespace RentifyxAssetRegistry.Api.Extensions;

public static class OutboxServiceCollectionExtensions
{
    public static IServiceCollection AddOutboxPublishing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OutboxOptions>(o =>
        {
            o.PollIntervalSeconds =
                configuration.GetValue<int?>(ConfigurationKeys.OutboxPollIntervalSeconds) ?? o.PollIntervalSeconds;
            o.BatchSize =
                configuration.GetValue<int?>(ConfigurationKeys.OutboxBatchSize) ?? o.BatchSize;
            o.MaxRetries =
                configuration.GetValue<int?>(ConfigurationKeys.OutboxMaxRetries) ?? o.MaxRetries;
        });

        services.AddSingleton<IProducer<string, string>>(_ =>
        {
            string bootstrapServers = configuration[ConfigurationKeys.KafkaBootstrapServers] ?? "localhost:9092";
            ProducerConfig producerConfig = new() { BootstrapServers = bootstrapServers };

            return new ProducerBuilder<string, string>(producerConfig).Build();
        });

        return services;
    }
}
