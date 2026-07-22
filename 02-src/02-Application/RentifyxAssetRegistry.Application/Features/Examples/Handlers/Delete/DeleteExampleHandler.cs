using ErrorOr;
using Microsoft.Extensions.Logging;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Domain.Constants;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Interfaces;
using RentifyxAssetRegistry.Domain.Interfaces.Common;

namespace RentifyxAssetRegistry.Application.Features.Examples.Handlers.Delete;

public sealed class DeleteExampleHandler(
    IGetByIdRepository<ExampleEntity> getByIdRepository,
    IDeleteRepository<ExampleEntity> deleteRepository,
    IUnitOfWork unitOfWork,
    ILogger<DeleteExampleHandler> logger) : IHandler<Guid, Deleted>
{
    public async Task<ErrorOr<Deleted>> HandleAsync(Guid id, CancellationToken ct = default)
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
