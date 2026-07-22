namespace rentifyx_asset_registry_api.Domain.Constants;

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
}
