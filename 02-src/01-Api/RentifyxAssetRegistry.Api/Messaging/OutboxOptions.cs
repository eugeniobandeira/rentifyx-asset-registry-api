namespace RentifyxAssetRegistry.Api.Messaging;

public sealed record OutboxOptions
{
    public int PollIntervalSeconds { get; init; } = 5;

    public int BatchSize { get; init; } = 25;

    public int MaxRetries { get; init; } = 3;
}
