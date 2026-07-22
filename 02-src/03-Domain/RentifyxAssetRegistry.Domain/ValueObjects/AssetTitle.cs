using RentifyxAssetRegistry.Domain.Constants;

namespace RentifyxAssetRegistry.Domain.ValueObjects;

public sealed record AssetTitle
{
    public string Value { get; }

    private AssetTitle(string value)
    {
        Value = value;
    }

    public static AssetTitle Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Length is < ValidationConstants.AssetRules.TitleMinLength or
            > ValidationConstants.AssetRules.TitleMaxLength)
        {
            throw new ArgumentException(
                $"Title must be between {ValidationConstants.AssetRules.TitleMinLength} and " +
                $"{ValidationConstants.AssetRules.TitleMaxLength} characters.",
                nameof(value));
        }

        return new AssetTitle(value);
    }
}
