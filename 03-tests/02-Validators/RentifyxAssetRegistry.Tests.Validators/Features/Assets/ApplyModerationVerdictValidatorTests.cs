using FluentAssertions;
using FluentValidation.Results;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ApplyModerationVerdict.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ApplyModerationVerdict.Validator;
using RentifyxAssetRegistry.Domain.Enums;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Validators.Features.Assets;

public sealed class ApplyModerationVerdictValidatorTests
{
    private readonly ApplyModerationVerdictValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        ValidationResult result = _validator.Validate(new ApplyModerationVerdictRequest(Guid.NewGuid(), ModerationVerdict.Approved));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyAssetId_ShouldFail()
    {
        ValidationResult result = _validator.Validate(new ApplyModerationVerdictRequest(Guid.Empty, ModerationVerdict.Approved));

        result.IsValid.Should().BeFalse();
    }
}
