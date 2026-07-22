using ErrorOr;
using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Common.Mapper;
using RentifyxAssetRegistry.Application.Features.Examples.Handlers.GetAll.Request;
using RentifyxAssetRegistry.Application.Features.Examples.Mapper;
using RentifyxAssetRegistry.Domain.Common;
using RentifyxAssetRegistry.Domain.Entities;

namespace RentifyxAssetRegistry.Api.Endpoints.Examples;

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
        ErrorOr<PagedResult<ExampleEntity>> result = await handler.HandleAsync(request, ct);

        return result.Match(
            pagedResult => Results.Ok(ApiListResponseMapper.ToListResponse(
                [.. pagedResult.Items.Select(ExampleMapper.ToResponse)],
                pagedResult.Total,
                request.Page,
                request.PageSize)),
            errors => errors.ToProblem(httpContext));
    }
}
