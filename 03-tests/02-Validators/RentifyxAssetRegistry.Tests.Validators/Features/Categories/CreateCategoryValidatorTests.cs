using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Create.Request;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Create.Validator;
using FluentAssertions;
using FluentValidation.Results;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Validators.Features.Categories;

public sealed class CreateCategoryValidatorTests
{
    private readonly CreateCategoryValidator _validator = new();

    [Fact]
    public void Validate_ValidRootRequest_ShouldPass()
    {
        CreateCategoryRequest request = new("Electronics", null, true);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyName_ShouldFail(string name)
    {
        CreateCategoryRequest request = new(name, null, true);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
