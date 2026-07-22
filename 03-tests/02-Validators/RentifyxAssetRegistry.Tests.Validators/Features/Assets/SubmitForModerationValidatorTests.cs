using FluentAssertions;
using FluentValidation.Results;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.SubmitForModeration.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.SubmitForModeration.Validator;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Validators.Features.Assets;

public sealed class SubmitForModerationValidatorTests
{
    private readonly SubmitForModerationValidator _validator = new();

    private static SubmitForModerationRequest ValidRequest() => new(Guid.NewGuid(), Guid.NewGuid());

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        ValidationResult result = _validator.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyAssetId_ShouldFail()
    {
        SubmitForModerationRequest request = ValidRequest() with { AssetId = Guid.Empty };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_EmptyOwnerId_ShouldFail()
    {
        SubmitForModerationRequest request = ValidRequest() with { OwnerId = Guid.Empty };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
