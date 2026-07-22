using rentifyx_asset_registry_api.Application.Common.Handler;
using rentifyx_asset_registry_api.Application.Extensions;
using rentifyx_asset_registry_api.Application.Features.Examples.Handlers.Update.Request;
using rentifyx_asset_registry_api.Application.Features.Examples.Mapper;
using rentifyx_asset_registry_api.Domain.Entities;
using rentifyx_asset_registry_api.Domain.Interfaces;
using rentifyx_asset_registry_api.Domain.Interfaces.Common;
using ErrorOr;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace rentifyx_asset_registry_api.Application.Features.Examples.Handlers.Update;

public sealed class UpdateExampleHandler(
    IGetByIdRepository<ExampleEntity> getByIdRepository,
    IUpdateRepository<ExampleEntity> updateRepository,
    IUnitOfWork unitOfWork,
    IValidator<UpdateExampleRequest> validator,
    ILogger<UpdateExampleHandler> logger) : IHandler<UpdateExampleRequest, ExampleEntity>
{
    public async Task<ErrorOr<ExampleEntity>> Handle(
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
