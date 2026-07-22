using FluentAssertions;
using FluentValidation.Results;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Search.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Search.Validator;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Validators.Features.Assets;

public sealed class SearchAssetsValidatorTests
{
    private readonly SearchAssetsValidator _validator = new();

    private static SearchAssetsRequest ValidRequest() => new(PageSize: 10);

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        ValidationResult result = _validator.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NoFilters_ShouldPass()
    {
        SearchAssetsRequest request = new(PageSize: 30);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(31)]
    public void Validate_PageSizeOutOfBounds_ShouldFail(int pageSize)
    {
        SearchAssetsRequest request = ValidRequest() with { PageSize = pageSize };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_MaxPageSize_ShouldPass()
    {
        SearchAssetsRequest request = ValidRequest() with { PageSize = 30 };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NegativeMinPrice_ShouldFail()
    {
        SearchAssetsRequest request = ValidRequest() with { MinPrice = -1m };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NegativeMaxPrice_ShouldFail()
    {
        SearchAssetsRequest request = ValidRequest() with { MaxPrice = -1m };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_MinPriceGreaterThanMaxPrice_ShouldFail()
    {
        SearchAssetsRequest request = ValidRequest() with { MinPrice = 100m, MaxPrice = 50m };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_MinPriceEqualsMaxPrice_ShouldPass()
    {
        SearchAssetsRequest request = ValidRequest() with { MinPrice = 50m, MaxPrice = 50m };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_BlankKeyword_ShouldPass()
    {
        SearchAssetsRequest request = ValidRequest() with { Keyword = "   " };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithCategoryIdAndKeyword_ShouldPass()
    {
        SearchAssetsRequest request = ValidRequest() with { CategoryId = Guid.NewGuid(), Keyword = "excavator" };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
