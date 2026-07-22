namespace rentifyx_asset_registry_api.Application.Features.Examples;

public sealed record ExampleResponse(
    Guid Id,
    string Name,
    string Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
