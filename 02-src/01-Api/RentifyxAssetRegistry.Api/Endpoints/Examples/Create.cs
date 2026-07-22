using ErrorOr;
using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using RentifyxAssetRegistry.Application.Features.Examples.Handlers.Create.Request;
using RentifyxAssetRegistry.Application.Features.Examples.Mapper;
using RentifyxAssetRegistry.Domain.Entities;

namespace RentifyxAssetRegistry.Api.Endpoints.Examples;

internal sealed class Create : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/examples", HandleAsync)
           .WithName("CreateExample")
           .WithDescription("Create a new example.")
           .WithTags(Tags.EXAMPLE);
    }

    private static async Task<IResult> HandleAsync(
        CreateExampleRequest request,
        IHandler<CreateExampleRequest, ExampleEntity> handler,
        HttpContext httpContext,
        CancellationToken ct = default)
    {
        ErrorOr<ExampleEntity> result = await handler.HandleAsync(request, ct);

        return result.Match(
            entity => Results.Created($"/api/v1/examples/{entity.Id}", ExampleMapper.ToResponse(entity)),
            errors => errors.ToProblem(httpContext));
    }
}
