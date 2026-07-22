using rentifyx_asset_registry_api.Application.Common.Handler;
using rentifyx_asset_registry_api.Application.Extensions;
using rentifyx_asset_registry_api.Application.Features.Examples.Handlers.GetAll.Request;
using rentifyx_asset_registry_api.Application.Features.Examples.Mapper;
using rentifyx_asset_registry_api.Domain.Common;
using rentifyx_asset_registry_api.Domain.Entities;
using rentifyx_asset_registry_api.Domain.Interfaces.Examples;
using ErrorOr;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace rentifyx_asset_registry_api.Application.Features.Examples.Handlers.GetAll;

public sealed class GetAllExampleHandler(
    IExampleRepository repository,
    IValidator<GetAllExampleRequest> validator,
    ILogger<GetAllExampleHandler> logger) : IHandler<GetAllExampleRequest, PagedResult<ExampleEntity>>
{
    public async Task<ErrorOr<PagedResult<ExampleEntity>>> Handle(GetAllExampleRequest request, CancellationToken ct = default)
    {
        logger.LogDebug("Fetching examples. Payload={@Payload}", request);

        List<Error>? errors = await validator.ValidateToErrorsAsync(request, ct);
        if (errors is not null)
            return errors;

        PagedResult<ExampleEntity> result = await repository.GetAllAsync(ExampleMapper.ToFilter(request), ct);

        logger.LogDebug("Fetched {Count} of {Total} examples.", result.Items.Count, result.Total);

        return ErrorOrFactory.From(result);
    }
}
