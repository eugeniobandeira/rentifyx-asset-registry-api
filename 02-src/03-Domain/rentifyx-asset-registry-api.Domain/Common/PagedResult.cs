namespace rentifyx_asset_registry_api.Domain.Common;

public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Total);
