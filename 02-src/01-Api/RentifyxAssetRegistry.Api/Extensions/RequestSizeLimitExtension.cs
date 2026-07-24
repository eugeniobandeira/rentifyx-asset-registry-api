using RentifyxAssetRegistry.Api.Configuration;
using RentifyxAssetRegistry.Api.Middlewares;
using RentifyxAssetRegistry.Infrastructure.Constants;

namespace RentifyxAssetRegistry.Api.Extensions;

internal static class RequestSizeLimitExtension
{
    // This API only ever receives JSON metadata bodies - file uploads go direct-to-S3 via
    // presigned URLs (ADR-AR-005), this API never receives raw file bytes - so 1 MB is a
    // generous ceiling for any request this service actually handles.
    internal const long DefaultMaxRequestBodyBytes = 1 * 1024 * 1024;

    public static IServiceCollection AddRequestSizeLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RequestSizeLimitOptions>(options =>
        {
            options.MaxRequestBodyBytes = configuration.GetValue(
                ConfigurationKeys.RequestSizeLimitMaxBytes,
                DefaultMaxRequestBodyBytes);
        });

        return services;
    }

    public static IApplicationBuilder UseRequestSizeLimiting(this IApplicationBuilder app)
        => app.UseMiddleware<RequestSizeLimitMiddleware>();
}
