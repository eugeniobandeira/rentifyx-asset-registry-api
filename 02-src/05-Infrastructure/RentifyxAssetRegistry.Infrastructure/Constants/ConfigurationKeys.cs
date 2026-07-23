namespace RentifyxAssetRegistry.Infrastructure.Constants;

public static class ConfigurationKeys
{
    public const string JwtPublicKeyPem = "Jwt:PublicKeyPem";
    public const string JwtIssuer = "Jwt:Issuer";
    public const string JwtAudience = "Jwt:Audience";
    public const string AwsRegion = "AWS:Region";
    public const string AwsSecretsManagerSecretName = "AWS:SecretsManager:SecretName";

    public const string DynamoDbTableName = "AWS:DynamoDb:TableName";
    public const string DynamoDbRegion = "AWS:DynamoDb:Region";
    public const string DynamoDbServiceUrl = "AWS:DynamoDb:ServiceUrl";

    public const string KafkaBootstrapServers = "Kafka:BootstrapServers";

    public const string OutboxPollIntervalSeconds = "Outbox:PollIntervalSeconds";
    public const string OutboxBatchSize = "Outbox:BatchSize";
    public const string OutboxMaxRetries = "Outbox:MaxRetries";

    public const string DefaultAwsRegion = "sa-east-1";
    public const string TestingEnvironment = "Testing";
}
