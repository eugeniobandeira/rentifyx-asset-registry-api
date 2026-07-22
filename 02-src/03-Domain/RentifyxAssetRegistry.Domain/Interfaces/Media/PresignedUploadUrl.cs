namespace RentifyxAssetRegistry.Domain.Interfaces.Media;

public sealed record PresignedUploadUrl(
    string Url,
    string S3Key
);
