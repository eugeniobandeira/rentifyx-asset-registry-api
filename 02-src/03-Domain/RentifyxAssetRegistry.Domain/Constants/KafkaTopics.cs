namespace RentifyxAssetRegistry.Domain.Constants;

public static class KafkaTopics
{
    public const string AssetCreated = "asset-registry.asset-created";
    public const string AssetMediaUploaded = "asset-registry.asset-media-uploaded";
    public const string AssetPublished = "asset-registry.asset-published";
    public const string AssetSuspended = "asset-registry.asset-suspended";

    // Inbound topics. Literal producer-side strings — the producing services don't follow this
    // repo's dotted naming convention, do not rename to match it.
    public const string UserLifecycleEvents = "user-lifecycle-events";
    public const string AssetMediaModerated = "asset-media-moderated";
}
