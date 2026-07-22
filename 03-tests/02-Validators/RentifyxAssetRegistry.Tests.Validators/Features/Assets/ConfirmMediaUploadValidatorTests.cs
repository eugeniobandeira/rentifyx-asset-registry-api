using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ConfirmMediaUpload.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.ConfirmMediaUpload.Validator;
using FluentAssertions;
using FluentValidation.Results;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Validators.Features.Assets;

public sealed class ConfirmMediaUploadValidatorTests
{
    private readonly ConfirmMediaUploadValidator _validator = new();

    private static ConfirmMediaUploadRequest ValidRequest() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "assets/owner/asset/photo.jpg",
        "image/jpeg",
        1024);

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        ValidationResult result = _validator.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyS3Key_ShouldFail()
    {
        ConfirmMediaUploadRequest request = ValidRequest() with { S3Key = "" };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_DisallowedMimeType_ShouldFail()
    {
        ConfirmMediaUploadRequest request = ValidRequest() with { MimeType = "application/exe" };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NonPositiveSizeBytes_ShouldFail()
    {
        ConfirmMediaUploadRequest request = ValidRequest() with { SizeBytes = 0 };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
