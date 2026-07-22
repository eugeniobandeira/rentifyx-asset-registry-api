namespace RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create.Request;

public sealed record CreateAssetRequest(
    Guid OwnerId,
    string Title,
    string Description,
    Guid CategoryId,
    string IdempotencyKey
);
