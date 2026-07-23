namespace RentifyxAssetRegistry.Domain.Constants;

public static class KafkaTopics
{
    public const string AssetCreated = "asset-registry.asset-created";
    public const string AssetMediaUploaded = "asset-registry.asset-media-uploaded";
    public const string AssetPublished = "asset-registry.asset-published";
    public const string AssetSuspended = "asset-registry.asset-suspended";
}
