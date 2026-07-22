namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create.Request;

public sealed record CreateAssetRequest(
    Guid OwnerId,
    string Title,
    string Description,
    decimal Price,
    Guid CategoryId,
    string IdempotencyKey
);
