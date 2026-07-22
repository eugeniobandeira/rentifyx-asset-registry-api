using FluentAssertions;
using FluentValidation.Results;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Update.Request;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Update.Validator;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Validators.Features.Categories;

public sealed class UpdateCategoryValidatorTests
{
    private readonly UpdateCategoryValidator _validator = new();

    [Fact]
    public void Validate_ValidRequestWithNewName_ShouldPass()
    {
        UpdateCategoryRequest request = new(Guid.NewGuid(), true, "New Name", null);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NoChangesRequested_ShouldPass()
    {
        UpdateCategoryRequest request = new(Guid.NewGuid(), true, null, null);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyCategoryId_ShouldFail()
    {
        UpdateCategoryRequest request = new(Guid.Empty, true, "New Name", null);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WhitespaceNewName_ShouldFail()
    {
        UpdateCategoryRequest request = new(Guid.NewGuid(), true, "   ", null);

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
