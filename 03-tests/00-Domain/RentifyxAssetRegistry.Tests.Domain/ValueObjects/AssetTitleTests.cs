using FluentAssertions;
using RentifyxAssetRegistry.Domain.ValueObjects;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Domain.ValueObjects;

public class AssetTitleTests
{
    [Fact]
    public void Create_ValidTitle_ShouldSucceed()
    {
        AssetTitle title = AssetTitle.Create("Valid Title");

        title.Value.Should().Be("Valid Title");
    }

    [Fact]
    public void Create_ExactlyMinLength_ShouldSucceed()
    {
        AssetTitle title = AssetTitle.Create("Abc");

        title.Value.Should().Be("Abc");
    }

    [Fact]
    public void Create_ExactlyMaxLength_ShouldSucceed()
    {
        string value = new('A', 100);

        AssetTitle title = AssetTitle.Create(value);

        title.Value.Should().Be(value);
    }

    [Fact]
    public void Create_TooShort_ShouldThrow()
    {
        Action act = () => AssetTitle.Create("Ab");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_TooLong_ShouldThrow()
    {
        string value = new('A', 101);

        Action act = () => AssetTitle.Create(value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_NullValue_ShouldThrow()
    {
        Action act = () => AssetTitle.Create(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WhitespaceValue_ShouldThrow()
    {
        Action act = () => AssetTitle.Create("   ");

        act.Should().Throw<ArgumentException>();
    }
}
