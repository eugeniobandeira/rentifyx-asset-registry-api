namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.ConfirmMediaUpload.Request;

public sealed record ConfirmMediaUploadRequest(
    Guid AssetId,
    Guid OwnerId,
    string S3Key,
    string MimeType,
    long SizeBytes
);
