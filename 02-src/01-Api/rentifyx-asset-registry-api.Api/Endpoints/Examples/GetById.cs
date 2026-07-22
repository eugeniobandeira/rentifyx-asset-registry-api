using rentifyx_asset_registry_api.Api.Abstract;
using rentifyx_asset_registry_api.Api.Extensions;
using rentifyx_asset_registry_api.Application.Common.Handler;
using rentifyx_asset_registry_api.Application.Features.Examples.Mapper;
using rentifyx_asset_registry_api.Domain.Entities;
using ErrorOr;

namespace rentifyx_asset_registry_api.Api.Endpoints.Examples;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/examples/{id:guid}", HandleAsync)
           .WithName("GetExampleById")
           .WithDescription("Get a example by id.")
           .WithTags(Tags.EXAMPLE);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        IHandler<Guid, ExampleEntity> handler,
        HttpContext httpContext,
        CancellationToken ct = default)
    {
        ErrorOr<ExampleEntity> result = await handler.Handle(id, ct);

        return result.Match(
            entity => Results.Ok(ExampleMapper.ToResponse(entity)),
            errors => errors.ToProblem(httpContext));
    }
}
