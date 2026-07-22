using FluentAssertions;
using RentifyxAssetRegistry.Domain.ValueObjects;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Domain.ValueObjects;

public class AssetDescriptionTests
{
    [Fact]
    public void Create_ValidDescription_ShouldSucceed()
    {
        AssetDescription description = AssetDescription.Create("A valid description over ten chars");

        description.Value.Should().Be("A valid description over ten chars");
    }

    [Fact]
    public void Create_ExactlyMinLength_ShouldSucceed()
    {
        string value = new('A', 10);

        AssetDescription description = AssetDescription.Create(value);

        description.Value.Should().Be(value);
    }

    [Fact]
    public void Create_ExactlyMaxLength_ShouldSucceed()
    {
        string value = new('A', 2000);

        AssetDescription description = AssetDescription.Create(value);

        description.Value.Should().Be(value);
    }

    [Fact]
    public void Create_TooShort_ShouldThrow()
    {
        Action act = () => AssetDescription.Create("short");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_TooLong_ShouldThrow()
    {
        string value = new('A', 2001);

        Action act = () => AssetDescription.Create(value);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_NullValue_ShouldThrow()
    {
        Action act = () => AssetDescription.Create(null!);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WhitespaceValue_ShouldThrow()
    {
        Action act = () => AssetDescription.Create("   ");

        act.Should().Throw<ArgumentException>();
    }
}
