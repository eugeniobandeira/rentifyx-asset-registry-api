namespace RentifyxAssetRegistry.Domain.Constants;

public static class ValidationConstants
{
    public static class ExampleRules
    {
        public const int NameMaxLength = 100;
        public const int DescriptionMaxLength = 500;
    }

    public static class Pagination
    {
        public const int MinPage = 1;
        public const int MinPageSize = 1;
        public const int MaxPageSize = 100;
    }

    public static class AssetRules
    {
        public const int TitleMinLength = 3;
        public const int TitleMaxLength = 100;
        public const int DescriptionMinLength = 10;
        public const int DescriptionMaxLength = 2000;
    }

    public static class CategoryRules
    {
        public const int MaxDepth = 3;
    }

    public static class SearchRules
    {
        public const int MinPageSize = 1;
        public const int MaxPageSize = 30;
    }

    public static class MediaRules
    {
        public static readonly IReadOnlySet<string> AllowedMimeTypes = new HashSet<string>
        {
            "image/jpeg",
            "image/png",
            "image/webp",
            "video/mp4"
        };
    }
}
