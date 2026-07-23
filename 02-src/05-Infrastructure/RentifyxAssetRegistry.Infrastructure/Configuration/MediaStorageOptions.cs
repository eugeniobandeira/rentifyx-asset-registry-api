namespace RentifyxAssetRegistry.Infrastructure.Configuration;

public sealed class MediaStorageOptions
{
    public string BucketName { get; set; } = string.Empty;

    public int PresignedUrlExpirySeconds { get; set; } = 900;
}
