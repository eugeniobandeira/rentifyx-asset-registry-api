namespace RentifyxAssetRegistry.Domain.Constants;

public static class CategoryErrorCodes
{
    public const string NotFound = "Category.NotFound";
    public const string NotAdmin = "Category.NotAdmin";
    public const string HasChildren = "Category.HasChildren";
    public const string MaxDepthExceeded = "Category.MaxDepthExceeded";
    public const string SelfParent = "Category.SelfParent";
}
