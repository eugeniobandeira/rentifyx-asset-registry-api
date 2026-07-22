namespace RentifyxAssetRegistry.Domain.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; }

    public string Currency { get; }

    private Money(decimal amount)
    {
        Amount = amount;
        Currency = "BRL";
    }

    public static Money Create(decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        return new Money(amount);
    }
}
