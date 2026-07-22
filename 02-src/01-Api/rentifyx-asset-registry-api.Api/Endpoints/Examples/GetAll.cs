using rentifyx_asset_registry_api.Api.Abstract;
using rentifyx_asset_registry_api.Api.Extensions;
using rentifyx_asset_registry_api.Application.Common.Handler;
using rentifyx_asset_registry_api.Application.Common.Mapper;
using rentifyx_asset_registry_api.Application.Features.Examples.Handlers.GetAll.Request;
using rentifyx_asset_registry_api.Application.Features.Examples.Mapper;
using rentifyx_asset_registry_api.Domain.Common;
using rentifyx_asset_registry_api.Domain.Entities;
using ErrorOr;

namespace rentifyx_asset_registry_api.Api.Endpoints.Examples;

internal sealed class GetAll : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/examples", HandleAsync)
           .WithName("GetAllExamples")
           .WithDescription("Get all active examples.")
           .WithTags(Tags.EXAMPLE);
    }

    private static async Task<IResult> HandleAsync(
        [AsParameters] GetAllExampleRequest request,
        IHandler<GetAllExampleRequest, PagedResult<ExampleEntity>> handler,
        HttpContext httpContext,
        CancellationToken ct = default)
    {
        ErrorOr<PagedResult<ExampleEntity>> result = await handler.Handle(request, ct);

        return result.Match(
            pagedResult => Results.Ok(ApiListResponseMapper.ToListResponse(
                [.. pagedResult.Items.Select(ExampleMapper.ToResponse)],
                pagedResult.Total,
                request.Page,
                request.PageSize)),
            errors => errors.ToProblem(httpContext));
    }
}
