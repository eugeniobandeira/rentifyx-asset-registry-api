using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace RentifyxAssetRegistry.Api.Extensions;

/// <summary>
/// Wires up OpenAPI 3.1 document generation (<c>Microsoft.AspNetCore.OpenApi</c>) and the
/// Scalar interactive API reference UI for the RentifyxAssetRegistry API.
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    /// Registers OpenAPI document generation and overrides the generated document's
    /// <c>info</c> section (title, description, contact) with values describing this service.
    /// </summary>
    /// <param name="services">The service collection to add OpenAPI generation to.</param>
    /// <param name="configuration">
    /// App configuration, used to read <c>OpenApi:ContactName</c> and <c>OpenApi:ContactUrl</c>.
    /// </param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services, IConfiguration configuration)
    {
        string contactName = configuration["OpenApi:ContactName"]!;
        string contactUrl = configuration["OpenApi:ContactUrl"]!;

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "RentifyxAssetRegistry API",
                    Version = "v1",
                    Description = "Asset catalog microservice for the RentifyX platform: asset creation, " +
                        "categorization, presigned-S3 media uploads, moderated publishing, and search. " +
                        "Built with .NET 10 Minimal APIs, Clean Architecture, and DDD; backed by DynamoDB " +
                        "and S3, with event-driven moderation and owner-status sync via Kafka.",
                    Contact = new OpenApiContact
                    {
                        Name = contactName,
                        Url = new Uri(contactUrl)
                    }
                };

                return Task.CompletedTask;
            });
        });

        return services;
    }

    /// <summary>
    /// Maps the generated OpenAPI JSON document endpoint and the Scalar UI that renders it.
    /// Only called for the Development environment (see <c>Program.cs</c>) — this API surface
    /// is not exposed outside local/dev use.
    /// </summary>
    /// <param name="app">The application to map the OpenAPI and Scalar endpoints onto.</param>
    /// <returns>The same <paramref name="app"/> instance, for chaining.</returns>
    public static IApplicationBuilder UseOpenApiDocumentation(this WebApplication app)
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.Title = "RentifyxAssetRegistry API";
            options.Theme = ScalarTheme.DeepSpace;
            options.TagSorter = TagSorter.Alpha;
            options.OperationSorter = OperationSorter.Alpha;
        });

        return app;
    }
}
