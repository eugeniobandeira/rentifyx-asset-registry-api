using Confluent.Kafka;
using RentifyxAssetRegistry.Infrastructure.Constants;

namespace RentifyxAssetRegistry.Api.Extensions;

public static class CrossServiceConsumingExtensions
{
    public const string OwnerStatusConsumerKey = "owner-status";
    public const string ModerationVerdictConsumerKey = "moderation-verdict";

    public static IServiceCollection AddCrossServiceConsuming(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string bootstrapServers = configuration[ConfigurationKeys.KafkaBootstrapServers] ?? "localhost:9092";

        services.AddKeyedSingleton<IConsumer<string, string>>(OwnerStatusConsumerKey, (_, _) =>
        {
            ConsumerConfig consumerConfig = new()
            {
                BootstrapServers = bootstrapServers,
                GroupId = configuration[ConfigurationKeys.KafkaOwnerStatusConsumerGroupId] ?? "asset-registry.owner-status-consumer",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            return new ConsumerBuilder<string, string>(consumerConfig).Build();
        });

        services.AddKeyedSingleton<IConsumer<string, string>>(ModerationVerdictConsumerKey, (_, _) =>
        {
            ConsumerConfig consumerConfig = new()
            {
                BootstrapServers = bootstrapServers,
                GroupId = configuration[ConfigurationKeys.KafkaModerationVerdictConsumerGroupId] ?? "asset-registry.moderation-verdict-consumer",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            return new ConsumerBuilder<string, string>(consumerConfig).Build();
        });

        return services;
    }
}
