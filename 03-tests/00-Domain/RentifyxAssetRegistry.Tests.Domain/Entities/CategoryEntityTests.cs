using FluentAssertions;
using RentifyxAssetRegistry.Domain.Entities;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Domain.Entities;

public class CategoryEntityTests
{
    [Fact]
    public void CreateRoot_ValidName_ProducesDepthOneWithNoParent()
    {
        CategoryEntity category = CategoryEntity.CreateRoot("Electronics");

        category.Depth.Should().Be(1);
        category.ParentCategoryId.Should().BeNull();
        category.Name.Should().Be("Electronics");
    }

    [Fact]
    public void CreateChild_FromDepthOneParent_ProducesDepthTwo()
    {
        CategoryEntity root = CategoryEntity.CreateRoot("Electronics");

        CategoryEntity child = CategoryEntity.CreateChild("Computers", root);

        child.Depth.Should().Be(2);
        child.ParentCategoryId.Should().Be(root.Id);
    }

    [Fact]
    public void CreateChild_FromDepthTwoParent_ProducesDepthThree()
    {
        CategoryEntity root = CategoryEntity.CreateRoot("Electronics");
        CategoryEntity depthTwo = CategoryEntity.CreateChild("Computers", root);

        CategoryEntity depthThree = CategoryEntity.CreateChild("Laptops", depthTwo);

        depthThree.Depth.Should().Be(3);
        depthThree.ParentCategoryId.Should().Be(depthTwo.Id);
    }

    [Fact]
    public void CreateChild_FromDepthThreeParent_ThrowsArgumentException()
    {
        CategoryEntity root = CategoryEntity.CreateRoot("Electronics");
        CategoryEntity depthTwo = CategoryEntity.CreateChild("Computers", root);
        CategoryEntity depthThree = CategoryEntity.CreateChild("Laptops", depthTwo);

        Action act = () => CategoryEntity.CreateChild("Gaming Laptops", depthThree);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRoot_NullOrWhitespaceName_ThrowsArgumentException(string? name)
    {
        Action act = () => CategoryEntity.CreateRoot(name!);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateChild_NullOrWhitespaceName_ThrowsArgumentException(string? name)
    {
        CategoryEntity root = CategoryEntity.CreateRoot("Electronics");

        Action act = () => CategoryEntity.CreateChild(name!, root);

        act.Should().Throw<ArgumentException>();
    }
}
