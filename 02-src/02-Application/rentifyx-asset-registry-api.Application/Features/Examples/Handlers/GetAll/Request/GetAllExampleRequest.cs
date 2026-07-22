namespace rentifyx_asset_registry_api.Application.Features.Examples.Handlers.GetAll.Request;

public sealed record GetAllExampleRequest(
    int Page,
    int PageSize,
    string? Name = null,
    bool? IsActive = null);
