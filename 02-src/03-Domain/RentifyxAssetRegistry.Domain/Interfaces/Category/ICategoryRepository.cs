using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Interfaces.Common;

namespace RentifyxAssetRegistry.Domain.Interfaces.Category;

public interface ICategoryRepository :
    IGetByIdRepository<CategoryEntity>,
    IGetAllRepository<CategoryEntity>,
    ISaveRepository<CategoryEntity>
{
}
