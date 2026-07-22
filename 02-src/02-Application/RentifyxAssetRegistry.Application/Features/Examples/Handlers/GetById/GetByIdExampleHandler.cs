using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Interfaces.Common;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace RentifyxAssetRegistry.Application.Features.Examples.Handlers.GetById;

public sealed class GetByIdExampleHandler(
    IGetByIdRepository<ExampleEntity> repository,
    ILogger<GetByIdExampleHandler> logger) : IHandler<Guid, ExampleEntity>
{
    public async Task<ErrorOr<ExampleEntity>> HandleAsync(Guid id, CancellationToken ct = default)
    {
        logger.LogDebug("Fetching example. Id={Id}", id);

        ExampleEntity? entity = await repository.GetByIdAsync(id, ct);

        if (entity is null)
        {
            logger.LogWarning("Example not found. Id={Id}", id);
            return Error.NotFound(ExampleErrorCodes.NotFound, $"Example {id} not found.");
        }

        logger.LogDebug("Example fetched successfully. Response={@Response}", entity);

        return entity;
    }
}
