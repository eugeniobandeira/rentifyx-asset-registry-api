using rentifyx_asset_registry_api.Api.Abstract;

namespace rentifyx_asset_registry_api.Api.Endpoints.Health;

internal sealed class HealthCheck : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/health", HandleAsync)
           .WithName("HealthCheck")
           .WithDescription("Returns whether the application is up and running.")
           .WithTags(Tags.HEALTH)
           .AllowAnonymous();
    }

    private static IResult HandleAsync() => Results.Ok(new { status = "healthy" });
}
