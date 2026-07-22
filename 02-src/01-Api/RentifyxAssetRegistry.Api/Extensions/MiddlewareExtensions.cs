using RentifyxAssetRegistry.Api.Middlewares;

namespace RentifyxAssetRegistry.Api.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();
}
