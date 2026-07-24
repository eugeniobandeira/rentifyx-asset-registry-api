using RentifyxAssetRegistry.Domain.Enums;

namespace RentifyxAssetRegistry.Application.Features.Assets;

public sealed record GetAssetByIdResponse(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    Guid CategoryId,
    Guid OwnerId,
    AssetStatus Status,
    DateTime CreatedAt
);
