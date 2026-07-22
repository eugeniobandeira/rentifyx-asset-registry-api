using FluentAssertions;
using RentifyxAssetRegistry.Domain.Enums;
using RentifyxAssetRegistry.Domain.ValueObjects;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Domain.ValueObjects;

public class MediaTests
{
    [Fact]
    public void Create_ValidMedia_ShouldSucceed()
    {
        Media media = Media.Create("assets/abc.jpg", "image/jpeg", 1024, MediaUploadStatus.Pending);

        media.S3Key.Should().Be("assets/abc.jpg");
        media.MimeType.Should().Be("image/jpeg");
        media.SizeBytes.Should().Be(1024);
        media.Status.Should().Be(MediaUploadStatus.Pending);
    }

    [Fact]
    public void Create_EmptyS3Key_ShouldThrow()
    {
        Action act = () => Media.Create(string.Empty, "image/jpeg", 1024, MediaUploadStatus.Pending);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WhitespaceS3Key_ShouldThrow()
    {
        Action act = () => Media.Create("   ", "image/jpeg", 1024, MediaUploadStatus.Pending);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_NonPositiveSizeBytes_ShouldThrow()
    {
        Action act = () => Media.Create("assets/abc.jpg", "image/jpeg", 0, MediaUploadStatus.Pending);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_NegativeSizeBytes_ShouldThrow()
    {
        Action act = () => Media.Create("assets/abc.jpg", "image/jpeg", -1, MediaUploadStatus.Pending);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_DisallowedMimeType_ShouldThrow()
    {
        Action act = () => Media.Create("assets/abc.txt", "text/plain", 1024, MediaUploadStatus.Pending);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_MixedCaseMimeType_ShouldNormalizeAndSucceed()
    {
        Media media = Media.Create("assets/abc.jpg", "Image/JPEG", 1024, MediaUploadStatus.Pending);

        media.MimeType.Should().Be("image/jpeg");
    }
}
