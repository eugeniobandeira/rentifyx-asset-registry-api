using RentifyxAssetRegistry.Api.Abstract;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Application.Common.Handler;
using ErrorOr;

namespace RentifyxAssetRegistry.Api.Endpoints.Examples;

internal sealed class Delete : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("/examples/{id:guid}", HandleAsync)
           .WithName("DeleteExample")
           .WithDescription("Delete a example.")
           .WithTags(Tags.EXAMPLE);
    }

    private static async Task<IResult> HandleAsync(
        Guid id,
        IHandler<Guid, Deleted> handler,
        HttpContext httpContext,
        CancellationToken ct = default)
    {
        ErrorOr<Deleted> result = await handler.HandleAsync(id, ct);

        return result.Match(
            _ => Results.NoContent(),
            errors => errors.ToProblem(httpContext));
    }
}
