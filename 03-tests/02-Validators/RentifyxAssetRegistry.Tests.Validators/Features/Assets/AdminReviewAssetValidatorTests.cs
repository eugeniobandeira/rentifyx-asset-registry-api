using FluentAssertions;
using FluentValidation.Results;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.AdminReviewAsset.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.AdminReviewAsset.Validator;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Validators.Features.Assets;

public sealed class AdminReviewAssetValidatorTests
{
    private readonly AdminReviewAssetValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        ValidationResult result = _validator.Validate(new AdminReviewAssetRequest(Guid.NewGuid(), true, true));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyAssetId_ShouldFail()
    {
        ValidationResult result = _validator.Validate(new AdminReviewAssetRequest(Guid.Empty, true, true));

        result.IsValid.Should().BeFalse();
    }
}
