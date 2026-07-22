namespace RentifyxAssetRegistry.Application.Features.Categories.Handlers.ListCategories.Request;

[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "S2094:Classes should not be empty",
    Justification = "Marker request type for the parameterless ListCategories query, matches IHandler<TRequest,TResponse>'s shape convention.")]
public sealed record ListCategoriesRequest;
