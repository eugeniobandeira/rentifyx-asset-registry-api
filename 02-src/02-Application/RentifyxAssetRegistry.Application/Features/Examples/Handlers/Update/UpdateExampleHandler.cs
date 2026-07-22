using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Extensions;
using RentifyxAssetRegistry.Application.Features.Examples.Handlers.Update.Request;
using RentifyxAssetRegistry.Application.Features.Examples.Mapper;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Interfaces;
using RentifyxAssetRegistry.Domain.Interfaces.Common;
using ErrorOr;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace RentifyxAssetRegistry.Application.Features.Examples.Handlers.Update;

public sealed class UpdateExampleHandler(
    IGetByIdRepository<ExampleEntity> getByIdRepository,
    IUpdateRepository<ExampleEntity> updateRepository,
    IUnitOfWork unitOfWork,
    IValidator<UpdateExampleRequest> validator,
    ILogger<UpdateExampleHandler> logger) : IHandler<UpdateExampleRequest, ExampleEntity>
{
    public async Task<ErrorOr<ExampleEntity>> HandleAsync(
        UpdateExampleRequest request,
        CancellationToken ct = default)
    {
        logger.LogInformation("Updating example. Payload={@Payload}", request);

        List<Error>? errors = await validator.ValidateToErrorsAsync(request, ct);
        if (errors is not null)
            return errors;

        ExampleEntity? entity = await getByIdRepository.GetByIdAsync(request.Id, ct);

        ExampleMapper.UpdateExample(entity!, request);

        await updateRepository.UpdateAsync(entity!, ct);
        await unitOfWork.CommitAsync(ct);

        logger.LogInformation("Example updated successfully. Response={@Response}", entity);

        return entity!;
    }
}
