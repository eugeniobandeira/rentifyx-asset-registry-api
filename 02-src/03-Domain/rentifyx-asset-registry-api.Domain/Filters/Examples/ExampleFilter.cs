namespace rentifyx_asset_registry_api.Domain.Filters.Examples;

public sealed record ExampleFilter(
    int Page,
    int PageSize,
    string? Name = null,
    bool? IsActive = null);
