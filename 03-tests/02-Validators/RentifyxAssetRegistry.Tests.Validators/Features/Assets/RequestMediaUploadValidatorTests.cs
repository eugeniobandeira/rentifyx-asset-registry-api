using FluentAssertions;
using FluentValidation.Results;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.RequestMediaUpload.Request;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.RequestMediaUpload.Validator;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Validators.Features.Assets;

public sealed class RequestMediaUploadValidatorTests
{
    private readonly RequestMediaUploadValidator _validator = new();

    private static RequestMediaUploadRequest ValidRequest() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        "image/jpeg",
        1024);

    [Fact]
    public void Validate_ValidRequest_ShouldPass()
    {
        ValidationResult result = _validator.Validate(ValidRequest());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyAssetId_ShouldFail()
    {
        RequestMediaUploadRequest request = ValidRequest() with { AssetId = Guid.Empty };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_DisallowedMimeType_ShouldFail()
    {
        RequestMediaUploadRequest request = ValidRequest() with { MimeType = "application/exe" };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_MixedCaseMimeType_ShouldPass()
    {
        RequestMediaUploadRequest request = ValidRequest() with { MimeType = "Image/JPEG" };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveSizeBytes_ShouldFail(long sizeBytes)
    {
        RequestMediaUploadRequest request = ValidRequest() with { SizeBytes = sizeBytes };

        ValidationResult result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
