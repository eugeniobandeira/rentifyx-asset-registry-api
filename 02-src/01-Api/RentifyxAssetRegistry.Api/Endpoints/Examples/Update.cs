using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Examples.Handlers.Update.Request;
using RentifyxAssetRegistry.Application.Features.Examples.Mapper;
using RentifyxAssetRegistry.Domain.Entities;
using ErrorOr;

namespace RentifyxAssetRegistry.Api.Endpoints.Examples;

internal sealed class Update : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/examples/{id:guid}", HandleAsync)
           .WithName("UpdateExample")
           .WithDescription("Update an existing example.")
           .WithTags(Tags.EXAMPLE);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        UpdateExampleRequest request,
        IHandler<UpdateExampleRequest, ExampleEntity> handler,
        HttpContext httpContext,
        CancellationToken ct = default)
    {
        ErrorOr<ExampleEntity> result = await handler.HandleAsync(request with { Id = id }, ct);

        return result.Match(
            entity => Results.Ok(ExampleMapper.ToResponse(entity)),
            errors => errors.ToProblem(httpContext));
    }
}
