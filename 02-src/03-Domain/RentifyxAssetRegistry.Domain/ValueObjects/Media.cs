using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Domain.Enums;

namespace RentifyxAssetRegistry.Domain.ValueObjects;

public sealed record Media
{
    public string S3Key { get; }
    public string MimeType { get; }
    public long SizeBytes { get; }
    public MediaUploadStatus Status { get; }

    private Media(
        string s3Key,
        string mimeType,
        long sizeBytes,
        MediaUploadStatus status)
    {
        S3Key = s3Key;
        MimeType = mimeType;
        SizeBytes = sizeBytes;
        Status = status;
    }

    public static Media Create(
        string s3Key,
        string mimeType,
        long sizeBytes,
        MediaUploadStatus status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(s3Key);

        if (sizeBytes <= 0)
        {
            throw new ArgumentException("Size in bytes must be positive.", nameof(sizeBytes));
        }

        string normalizedMimeType = mimeType.ToLowerInvariant();

        if (!ValidationConstants.MediaRules.AllowedMimeTypes.Contains(normalizedMimeType))
        {
            throw new ArgumentException($"MIME type '{mimeType}' is not allowed.", nameof(mimeType));
        }

        return new Media(s3Key, normalizedMimeType, sizeBytes, status);
    }
}
