using RentifyxAssetRegistry.Domain.Common;
using RentifyxAssetRegistry.Domain.Constants;

namespace RentifyxAssetRegistry.Domain.Entities;

public sealed class CategoryEntity : AggregateRoot
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Guid? ParentCategoryId { get; private set; }
    public int Depth { get; private set; }

    private CategoryEntity() { }

    public static CategoryEntity CreateRoot(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new CategoryEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            ParentCategoryId = null,
            Depth = 1
        };
    }

    public static CategoryEntity CreateChild(
        string name,
        CategoryEntity parent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(parent);

        if (parent.Depth >= ValidationConstants.CategoryRules.MaxDepth)
        {
            throw new ArgumentException(
                $"Cannot create a child category under a parent at depth {parent.Depth}; max depth is {ValidationConstants.CategoryRules.MaxDepth}.",
                nameof(parent));
        }

        return new CategoryEntity
        {
            Id = Guid.NewGuid(),
            Name = name,
            ParentCategoryId = parent.Id,
            Depth = parent.Depth + 1
        };
    }
}
