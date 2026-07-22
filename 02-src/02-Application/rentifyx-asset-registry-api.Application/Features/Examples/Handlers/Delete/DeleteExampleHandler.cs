using rentifyx_asset_registry_api.Application.Common.Handler;
using rentifyx_asset_registry_api.Domain.Constants;
using rentifyx_asset_registry_api.Domain.Entities;
using rentifyx_asset_registry_api.Domain.Interfaces;
using rentifyx_asset_registry_api.Domain.Interfaces.Common;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace rentifyx_asset_registry_api.Application.Features.Examples.Handlers.Delete;

public sealed class DeleteExampleHandler(
    IGetByIdRepository<ExampleEntity> getByIdRepository,
    IDeleteRepository<ExampleEntity> deleteRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeleteExampleHandler> logger) : IHandler<Guid, Deleted>
{
    public async Task<ErrorOr<Deleted>> Handle(Guid id, CancellationToken ct = default)
    {
        logger.LogInformation("Deleting example. Id={Id}", id);

        ExampleEntity? entity = await getByIdRepository.GetByIdAsync(id, ct);

        if (entity is null)
        {
            logger.LogWarning("Example not found for deletion. Id={Id}", id);
            return Error.NotFound(ExampleErrorCodes.NotFound, $"Example {id} not found.");
        }

        await deleteRepository.DeleteAsync(entity, ct);
        await unitOfWork.CommitAsync(ct);

        logger.LogInformation("Example deleted successfully. Response={@Response}", entity);

        return Result.Deleted;
    }
}
