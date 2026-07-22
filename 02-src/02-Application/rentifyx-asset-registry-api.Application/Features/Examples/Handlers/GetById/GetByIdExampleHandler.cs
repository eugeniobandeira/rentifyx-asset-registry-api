using rentifyx_asset_registry_api.Application.Common.Handler;
using rentifyx_asset_registry_api.Domain.Constants;
using rentifyx_asset_registry_api.Domain.Entities;
using rentifyx_asset_registry_api.Domain.Interfaces.Common;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace rentifyx_asset_registry_api.Application.Features.Examples.Handlers.GetById;

public sealed class GetByIdExampleHandler(
    IGetByIdRepository<ExampleEntity> repository,
    ILogger<GetByIdExampleHandler> logger) : IHandler<Guid, ExampleEntity>
{
    public async Task<ErrorOr<ExampleEntity>> Handle(Guid id, CancellationToken ct = default)
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
