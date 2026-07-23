using System.Net;
using Amazon.S3;
using DotNet.Testcontainers.Builders;
using FluentAssertions;
using Microsoft.Extensions.Options;
using RentifyxAssetRegistry.Domain.Enums;
using RentifyxAssetRegistry.Domain.Interfaces.Media;
using RentifyxAssetRegistry.Infrastructure.Configuration;
using RentifyxAssetRegistry.Infrastructure.Storage;
using Testcontainers.LocalStack;
using Xunit;
using MediaValueObject = RentifyxAssetRegistry.Domain.ValueObjects.Media;

namespace RentifyxAssetRegistry.Tests.Repositories;

public sealed class S3MediaStorageServiceTests : IAsyncLifetime
{
    private const string BucketName = "rentifyx-media-test";

    private readonly LocalStackContainer _localStackContainer = new LocalStackBuilder("localstack/localstack:3.8")
        .Build();

    private IAmazonS3 _s3Client = null!;
    private S3MediaStorageService _sut = null!;

    public async Task InitializeAsync()
    {
        await _localStackContainer.StartAsync();

        AmazonS3Config s3Config = new()
        {
            ServiceURL = _localStackContainer.GetConnectionString(),
            ForcePathStyle = true
        };

        _s3Client = new AmazonS3Client("test", "test", s3Config);

        await _localStackContainer.ExecAsync(["awslocal", "s3", "mb", $"s3://{BucketName}"]);

        IOptions<MediaStorageOptions> options = Options.Create(new MediaStorageOptions
        {
            BucketName = BucketName,
            PresignedUrlExpirySeconds = 900
        });

        _sut = new S3MediaStorageService(_s3Client, options);
    }

    public async Task DisposeAsync()
    {
        _s3Client.Dispose();
        await _localStackContainer.DisposeAsync();
    }

    [Fact]
    public async Task HappyPath_GeneratesPresignedUrlUploadsAndValidatesSuccessfully()
    {
        Guid ownerId = Guid.NewGuid();
        Guid assetId = Guid.NewGuid();
        const string mimeType = "image/jpeg";
        byte[] content = "fake-image-bytes"u8.ToArray();

        PresignedUploadUrl presignedUploadUrl = await _sut.GeneratePresignedUploadUrlAsync(
            ownerId,
            assetId,
            mimeType,
            content.Length);

        presignedUploadUrl.S3Key.Should().StartWith($"assets/{ownerId}/{assetId}/");
        presignedUploadUrl.S3Key.Should().EndWith(".jpg");
        presignedUploadUrl.Url.Should().Contain("X-Amz-Signature");

        // LocalStack serves its TLS endpoint with a self-signed certificate; trusting it here
        // is a test-only concern (the emulator's cert, not a production S3 endpoint).
        using HttpClientHandler httpClientHandler = new()
        {
            CheckCertificateRevocationList = true,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        using HttpClient httpClient = new(httpClientHandler);
        using ByteArrayContent httpContent = new(content);
        httpContent.Headers.Add("Content-Type", mimeType);

        using HttpResponseMessage putResponse = await httpClient.PutAsync(presignedUploadUrl.Url, httpContent);
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        MediaValueObject uploadedMedia = MediaValueObject.Create(
            presignedUploadUrl.S3Key,
            mimeType,
            content.Length,
            MediaUploadStatus.Uploaded);

        bool uploadedIsValid = await _sut.ValidateUploadAsync(uploadedMedia);
        uploadedIsValid.Should().BeTrue();

        MediaValueObject unrelatedMedia = MediaValueObject.Create(
            $"assets/{ownerId}/{assetId}/{Guid.NewGuid()}.jpg",
            mimeType,
            content.Length,
            MediaUploadStatus.Pending);

        bool unrelatedIsValid = await _sut.ValidateUploadAsync(unrelatedMedia);
        unrelatedIsValid.Should().BeFalse();
    }
}
