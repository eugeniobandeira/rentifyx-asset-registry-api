namespace RentifyxAssetRegistry.Application.Features.Assets;

public sealed record RequestMediaUploadResponse(
    string UploadUrl,
    string S3Key
);
