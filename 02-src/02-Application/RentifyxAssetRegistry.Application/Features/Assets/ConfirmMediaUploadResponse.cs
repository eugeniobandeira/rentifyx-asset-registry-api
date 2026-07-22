namespace RentifyxAssetRegistry.Application.Features.Assets;

public sealed record ConfirmMediaUploadResponse(
    Guid AssetId,
    string S3Key
);
