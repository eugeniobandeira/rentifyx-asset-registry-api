using RentifyxAssetRegistry.Domain.Constants;

namespace RentifyxAssetRegistry.Domain.ValueObjects;

public sealed record AssetDescription
{
    public string Value { get; }

    private AssetDescription(string value)
    {
        Value = value;
    }

    public static AssetDescription Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Length is < ValidationConstants.AssetRules.DescriptionMinLength or
            > ValidationConstants.AssetRules.DescriptionMaxLength)
        {
            throw new ArgumentException(
                $"Description must be between {ValidationConstants.AssetRules.DescriptionMinLength} and {ValidationConstants.AssetRules.DescriptionMaxLength} characters.",
                nameof(value));
        }

        return new AssetDescription(value);
    }
}
