using RentifyxAssetRegistry.Domain.Interfaces.Media;
using RentifyxAssetRegistry.Domain.ValueObjects;

namespace RentifyxAssetRegistry.Tests.Integration.Fakes;

public sealed class FakeMediaStorageService : IMediaStorageService
{
    public Task<PresignedUploadUrl> GeneratePresignedUploadUrlAsync(
        Guid ownerId,
        Guid assetId,
        string mimeType,
        long sizeBytes,
        CancellationToken cancellationToken = default)
    {
        string key = $"assets/{ownerId}/{assetId}/fake-upload";

        return Task.FromResult(new PresignedUploadUrl($"https://fake-bucket.local/{key}", key));
    }

    public Task<bool> ValidateUploadAsync(Media media, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}
