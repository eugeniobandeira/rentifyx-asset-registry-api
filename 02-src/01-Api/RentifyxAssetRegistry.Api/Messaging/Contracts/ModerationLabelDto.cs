namespace RentifyxAssetRegistry.Api.Messaging.Contracts;

/// <summary>
/// Mirrors rentifyx-ai-services' ModerationLabel record. Deserialized for payload completeness,
/// never forwarded — ApplyModerationVerdictHandler's input surface has no room for it.
/// </summary>
public sealed record ModerationLabelDto(string Name, float Confidence);
