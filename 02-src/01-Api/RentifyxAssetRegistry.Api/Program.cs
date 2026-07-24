using System.Globalization;
using RentifyxAssetRegistry.Api.Extensions;
using RentifyxAssetRegistry.Api.Messaging;
using RentifyxAssetRegistry.Api.Middlewares;
using RentifyxAssetRegistry.Infrastructure.Configuration;
using RentifyxAssetRegistry.IoC;
using RentifyxAssetRegistry.ServiceDefaults;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.AddServiceDefaults();

    builder.Configuration.AddSecretsManager(builder.Configuration);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithThreadId()
        .WriteTo.Console(
            formatProvider: CultureInfo.InvariantCulture,
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddOpenApiDocumentation(builder.Configuration);
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddCorsPolicy(builder.Configuration);
    builder.Services.AddVersioning();
    builder.Services.AddRateLimiting(builder.Configuration);
    builder.Services.AddEndpoints();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    builder.Services.AddOutboxPublishing(builder.Configuration);
    builder.Services.AddHostedService<OutboxPublisher>();
    builder.Services.AddCrossServiceConsuming(builder.Configuration);
    builder.Services.AddHostedService<OwnerStatusConsumer>();
    builder.Services.AddHostedService<ModerationVerdictConsumer>();

    WebApplication app = builder.Build();

    app.MapDefaultEndpoints();

    app.UseExceptionHandler();
    app.UseSecurityHeaders();
    app.UseCorrelationId();
    app.UseRateLimiting();
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        };
    });

    if (app.Environment.IsDevelopment())
        app.UseOpenApiDocumentation();

    if (!app.Environment.IsDevelopment())
    {
        // HSTS follows the same env gate as UseHttpsRedirection below - both are meaningless (and
        // HSTS is actively wrong) over plain HTTP in local Development.
        app.UseHsts();
        app.UseHttpsRedirection();
    }

    app.UseCorsPolicy();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapEndpoints();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
