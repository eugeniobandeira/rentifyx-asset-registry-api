using rentifyx_asset_registry_api.Domain.Entities;
using rentifyx_asset_registry_api.Domain.Filters.Examples;
using rentifyx_asset_registry_api.Domain.Interfaces.Common;

namespace rentifyx_asset_registry_api.Domain.Interfaces.Examples;

public interface IExampleRepository :
    IAddRepository<ExampleEntity>,
    IGetByIdRepository<ExampleEntity>,
    IGetAllRepository<ExampleEntity, ExampleFilter>,
    IUpdateRepository<ExampleEntity>,
    IDeleteRepository<ExampleEntity>
{
}
