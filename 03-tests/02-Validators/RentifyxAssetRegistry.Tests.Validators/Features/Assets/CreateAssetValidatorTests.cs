using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create.Validator;
using FluentAssertions;
using FluentValidation.Results;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Validators.Features.Assets;

public sealed class CreateAssetValidatorTests
{
    private readonly CreateAssetValidator _validator = new();

    private static CreateAssetRequest ValidRequest() => new(
        Guid.NewGuid(),
        "Excavator CAT 320",
        "Heavy duty excavator available for rent.",
        Guid.NewGuid(),
        Guid.NewGuid().ToString());

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        ValidationResult result = _validator.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyOwnerId_ShouldFail()
    {
        CreateAssetRequest request = ValidRequest() with { OwnerId = Guid.Empty };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_EmptyCategoryId_ShouldFail()
    {
        CreateAssetRequest request = ValidRequest() with { CategoryId = Guid.Empty };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_MissingIdempotencyKey_ShouldFail()
    {
        CreateAssetRequest request = ValidRequest() with { IdempotencyKey = "" };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public void Validate_TitleTooShort_ShouldFail(string title)
    {
        CreateAssetRequest request = ValidRequest() with { Title = title };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_TitleTooLong_ShouldFail()
    {
        CreateAssetRequest request = ValidRequest() with { Title = new string('a', 101) };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public void Validate_DescriptionTooShort_ShouldFail(string description)
    {
        CreateAssetRequest request = ValidRequest() with { Description = description };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_DescriptionTooLong_ShouldFail()
    {
        CreateAssetRequest request = ValidRequest() with { Description = new string('a', 2001) };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
