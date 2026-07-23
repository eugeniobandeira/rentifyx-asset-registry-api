namespace RentifyxAssetRegistry.Infrastructure.Configuration;

public sealed record DynamoDbOptions
{
    public string TableName { get; init; } = "AssetRegistry";

    public string Region { get; init; } = "sa-east-1";

    public string? ServiceUrl { get; init; }
}
