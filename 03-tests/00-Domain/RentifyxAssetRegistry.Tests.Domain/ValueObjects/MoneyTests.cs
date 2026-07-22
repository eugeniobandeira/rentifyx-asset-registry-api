using FluentAssertions;
using RentifyxAssetRegistry.Domain.ValueObjects;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Domain.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_NegativeAmount_ShouldThrow()
    {
        Action act = () => Money.Create(-1m);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ZeroAmount_ShouldSucceed()
    {
        Money money = Money.Create(0m);

        money.Amount.Should().Be(0m);
    }

    [Fact]
    public void Create_PositiveAmount_ShouldSucceed()
    {
        Money money = Money.Create(150.75m);

        money.Amount.Should().Be(150.75m);
    }

    [Fact]
    public void Currency_AfterCreate_ShouldAlwaysBeBrl()
    {
        Money money = Money.Create(500m);

        money.Currency.Should().Be("BRL");
    }

    [Fact]
    public void Currency_ForZeroAmount_ShouldAlwaysBeBrl()
    {
        Money money = Money.Create(0m);

        money.Currency.Should().Be("BRL");
    }
}
