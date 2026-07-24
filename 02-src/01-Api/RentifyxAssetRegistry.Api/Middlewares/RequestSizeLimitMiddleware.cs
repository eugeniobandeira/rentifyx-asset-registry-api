using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using RentifyxAssetRegistry.Api.Configuration;

namespace RentifyxAssetRegistry.Api.Middlewares;

public sealed class RequestSizeLimitMiddleware(RequestDelegate next, IOptions<RequestSizeLimitOptions> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        long maxRequestBodyBytes = options.Value.MaxRequestBodyBytes;

        // Kestrel enforces this transport-wide via KestrelServerOptions.Limits.MaxRequestBodySize
        // (Program.cs), but the per-request feature also needs updating so it stays consistent for
        // hosts/tests that don't go through Kestrel (e.g. TestServer in integration tests).
        IHttpMaxRequestBodySizeFeature? sizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (sizeFeature is { IsReadOnly: false })
            sizeFeature.MaxRequestBodySize = maxRequestBodyBytes;

        if (context.Request.ContentLength > maxRequestBodyBytes)
        {
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            return;
        }

        await next(context);
    }
}
