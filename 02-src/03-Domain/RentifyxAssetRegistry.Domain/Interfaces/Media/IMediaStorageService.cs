using RentifyxAssetRegistry.Domain.ValueObjects;

namespace RentifyxAssetRegistry.Domain.Interfaces.Media;

public interface IMediaStorageService
{
    Task<PresignedUploadUrl> GeneratePresignedUploadUrlAsync(
        Guid ownerId,
        Guid assetId,
        string mimeType,
        long sizeBytes,
        CancellationToken cancellationToken = default);

    Task<bool> ValidateUploadAsync(global::RentifyxAssetRegistry.Domain.ValueObjects.Media media, CancellationToken cancellationToken = default);
}
