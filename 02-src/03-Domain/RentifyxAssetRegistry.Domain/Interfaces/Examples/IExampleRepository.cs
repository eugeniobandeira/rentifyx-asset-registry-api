using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Filters.Examples;
using RentifyxAssetRegistry.Domain.Interfaces.Common;

namespace RentifyxAssetRegistry.Domain.Interfaces.Examples;

public interface IExampleRepository :
    IAddRepository<ExampleEntity>,
    IGetByIdRepository<ExampleEntity>,
    IGetAllRepository<ExampleEntity, ExampleFilter>,
    IUpdateRepository<ExampleEntity>,
    IDeleteRepository<ExampleEntity>
{
}
