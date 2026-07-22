namespace RentifyxAssetRegistry.Domain.Common;

public sealed record CursorPagedResult<T>(IReadOnlyCollection<T> Items, string? NextPageToken);
