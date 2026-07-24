namespace RentifyxAssetRegistry.Api.Middlewares;

public sealed class SecurityHeadersMiddleware(RequestDelegate next, IWebHostEnvironment environment)
{
    private const string ContentTypeOptionsHeader = "X-Content-Type-Options";
    private const string ContentTypeOptionsValue = "nosniff";

    private const string FrameOptionsHeader = "X-Frame-Options";
    private const string FrameOptionsValue = "DENY";

    private const string ReferrerPolicyHeader = "Referrer-Policy";
    private const string ReferrerPolicyValue = "strict-origin-when-cross-origin";

    private const string ContentSecurityPolicyHeader = "Content-Security-Policy";

    // This is an API-only service - every response is JSON, so a locked-down 'default-src none'
    // CSP is correct almost everywhere. The one exception is the Scalar API docs UI
    // (OpenApiExtensions.UseOpenApiDocumentation), which is HTML with inline scripts/styles and is
    // only ever mapped when app.Environment.IsDevelopment() is true (see Program.cs). Rather than
    // special-case the /scalar and /openapi routes, we scope the strict CSP to non-Development
    // environments entirely - the same convention already used for UseHsts/UseHttpsRedirection in
    // Program.cs - so Scalar keeps working locally and every other environment gets the strict policy.
    private const string ContentSecurityPolicyValue = "default-src 'none'";

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[ContentTypeOptionsHeader] = ContentTypeOptionsValue;
            context.Response.Headers[FrameOptionsHeader] = FrameOptionsValue;
            context.Response.Headers[ReferrerPolicyHeader] = ReferrerPolicyValue;

            if (!environment.IsDevelopment())
                context.Response.Headers[ContentSecurityPolicyHeader] = ContentSecurityPolicyValue;

            return Task.CompletedTask;
        });

        await next(context);
    }
}
