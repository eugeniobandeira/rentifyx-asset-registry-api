using RentifyxAssetRegistry.Domain.Entities;

namespace RentifyxAssetRegistry.Domain.Interfaces.Category;

public interface ICategoryRepository
{
    Task<CategoryEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CategoryEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(CategoryEntity category, CancellationToken cancellationToken = default);
}
