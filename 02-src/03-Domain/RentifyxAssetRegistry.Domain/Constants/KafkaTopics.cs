namespace RentifyxAssetRegistry.Domain.Constants;

public static class KafkaTopics
{
    public const string AssetCreated = "asset-registry.asset-created";
    public const string AssetMediaUploaded = "asset-registry.asset-media-uploaded";
    public const string AssetPublished = "asset-registry.asset-published";
    public const string AssetSuspended = "asset-registry.asset-suspended";

    // Inbound topics. Literal producer-side strings, verified against the real identity-api/
    // rentifyx-ai-services code (F-12 spec.md) — do not rename to match this repo's own
    // dotted convention, the producing services do not follow it.
    public const string UserLifecycleEvents = "user-lifecycle-events";
    public const string AssetMediaModerated = "asset-media-moderated";
}
