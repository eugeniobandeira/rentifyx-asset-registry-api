namespace RentifyxAssetRegistry.Infrastructure.Constants;

public static class ConfigurationKeys
{
    public const string JwtPublicKeyPem = "Jwt:PublicKeyPem";
    public const string JwtIssuer = "Jwt:Issuer";
    public const string JwtAudience = "Jwt:Audience";
    public const string AwsRegion = "AWS:Region";
    public const string AwsSecretsManagerSecretName = "AWS:SecretsManager:SecretName";

    public const string DefaultAwsRegion = "sa-east-1";
    public const string TestingEnvironment = "Testing";
}
