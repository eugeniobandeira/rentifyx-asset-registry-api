namespace RentifyxAssetRegistry.Api.Messaging;

public sealed record OutboxOptions
{
    public int PollIntervalSeconds { get; set; } = 5;

    public int BatchSize { get; set; } = 25;

    public int MaxRetries { get; set; } = 3;
}
