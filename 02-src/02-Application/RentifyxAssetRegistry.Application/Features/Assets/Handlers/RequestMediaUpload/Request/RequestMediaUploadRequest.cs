namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.RequestMediaUpload.Request;

public sealed record RequestMediaUploadRequest(
    Guid AssetId,
    Guid OwnerId,
    string MimeType,
    long SizeBytes
);
