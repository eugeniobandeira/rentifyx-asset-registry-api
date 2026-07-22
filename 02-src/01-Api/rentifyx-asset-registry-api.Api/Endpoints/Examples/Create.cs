using rentifyx_asset_registry_api.Api.Abstract;
using rentifyx_asset_registry_api.Api.Extensions;
using rentifyx_asset_registry_api.Application.Common.Handler;
using rentifyx_asset_registry_api.Application.Features.Examples.Handlers.Create.Request;
using rentifyx_asset_registry_api.Application.Features.Examples.Mapper;
using rentifyx_asset_registry_api.Domain.Entities;
using ErrorOr;

namespace rentifyx_asset_registry_api.Api.Endpoints.Examples;

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
        ErrorOr<ExampleEntity> result = await handler.Handle(request, ct);

        return result.Match(
            entity => Results.Created($"/api/v1/examples/{entity.Id}", ExampleMapper.ToResponse(entity)),
            errors => errors.ToProblem(httpContext));
    }
}
