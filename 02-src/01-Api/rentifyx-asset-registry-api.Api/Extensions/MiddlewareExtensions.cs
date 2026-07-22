using rentifyx_asset_registry_api.Api.Middlewares;

namespace rentifyx_asset_registry_api.Api.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();
}
