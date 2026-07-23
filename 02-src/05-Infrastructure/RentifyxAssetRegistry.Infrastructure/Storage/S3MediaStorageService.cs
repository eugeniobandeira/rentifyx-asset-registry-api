using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using RentifyxAssetRegistry.Domain.Interfaces.Media;
using RentifyxAssetRegistry.Infrastructure.Configuration;

namespace RentifyxAssetRegistry.Infrastructure.Storage;

public sealed class S3MediaStorageService(
    IAmazonS3 client,
    IOptions<MediaStorageOptions> options) : IMediaStorageService
{
    private static readonly IReadOnlyDictionary<string, string> MimeTypeExtensions = new Dictionary<string, string>
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp",
        ["video/mp4"] = "mp4"
    };

    private const string DefaultExtension = "bin";

    public async Task<PresignedUploadUrl> GeneratePresignedUploadUrlAsync(
        Guid ownerId,
        Guid assetId,
        string mimeType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        MediaStorageOptions storageOptions = options.Value;
        string extension = ResolveExtension(mimeType);
        string filename = $"{Guid.NewGuid()}.{extension}";
        string s3Key = $"assets/{ownerId}/{assetId}/{filename}";

        GetPreSignedUrlRequest request = new()
        {
            BucketName = storageOptions.BucketName,
            Key = s3Key,
            Verb = HttpVerb.PUT,
            Expires = DateTime.UtcNow.AddSeconds(storageOptions.PresignedUrlExpirySeconds),
            ContentType = mimeType
        };

        string url = await client.GetPreSignedURLAsync(request);

        return new PresignedUploadUrl(url, s3Key);
    }

    public async Task<bool> ValidateUploadAsync(
        global::RentifyxAssetRegistry.Domain.ValueObjects.Media media,
        CancellationToken cancellationToken = default)
    {
        MediaStorageOptions storageOptions = options.Value;

        GetObjectMetadataRequest request = new()
        {
            BucketName = storageOptions.BucketName,
            Key = media.S3Key
        };

        try
        {
            GetObjectMetadataResponse response = await client.GetObjectMetadataAsync(request, cancellationToken);

            return response.ContentLength == media.SizeBytes;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private static string ResolveExtension(string mimeType)
        => MimeTypeExtensions.TryGetValue(mimeType.ToLowerInvariant(), out string? extension)
            ? extension
            : DefaultExtension;
}
