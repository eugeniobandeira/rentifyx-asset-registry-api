using rentifyx_asset_registry_api.Application.Features.Examples.Handlers.Create.Request;
using rentifyx_asset_registry_api.Application.Features.Examples.Handlers.GetAll.Request;
using rentifyx_asset_registry_api.Application.Features.Examples.Handlers.Update.Request;
using rentifyx_asset_registry_api.Domain.Entities;
using rentifyx_asset_registry_api.Domain.Filters.Examples;

namespace rentifyx_asset_registry_api.Application.Features.Examples.Mapper;

public static class ExampleMapper
{
    public static ExampleEntity CreateExample(CreateExampleRequest request)
        => ExampleEntity.Create(request.Name, request.Description);

    public static void UpdateExample(ExampleEntity entity, UpdateExampleRequest request)
        => entity.Update(request.Name, request.Description);

    public static ExampleResponse ToResponse(ExampleEntity entity)
        => new(entity.Id, entity.Name, entity.Description, entity.IsActive, entity.CreatedAt, entity.UpdatedAt);

    public static ExampleFilter ToFilter(GetAllExampleRequest request)
        => new(request.Page, request.PageSize, request.Name, request.IsActive);
}
