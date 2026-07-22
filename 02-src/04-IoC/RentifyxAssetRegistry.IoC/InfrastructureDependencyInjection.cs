using System.Security.Cryptography;
using RentifyxAssetRegistry.Domain.Entities;
using RentifyxAssetRegistry.Domain.Interfaces;
using RentifyxAssetRegistry.Domain.Interfaces.Common;
using RentifyxAssetRegistry.Domain.Interfaces.Examples;
using RentifyxAssetRegistry.Infrastructure.Constants;
using RentifyxAssetRegistry.Infrastructure.Context;
using RentifyxAssetRegistry.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace RentifyxAssetRegistry.IoC;

internal static class InfrastructureDependencyInjection
{
    internal static IServiceCollection Register(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext(configuration);
        services.AddRepositories();
        services.AddJwtAuthentication(configuration);

        return services;
    }

    private static void AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                string? pem = configuration[ConfigurationKeys.JwtPublicKeyPem];

                TokenValidationParameters parameters = new()
                {
                    ValidIssuer = configuration[ConfigurationKeys.JwtIssuer],
                    ValidAudience = configuration[ConfigurationKeys.JwtAudience],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                if (!string.IsNullOrWhiteSpace(pem))
                {
                    RSA rsa = RSA.Create();
                    rsa.ImportFromPem(pem.AsSpan());
                    parameters.IssuerSigningKey = new RsaSecurityKey(rsa);
                }

                options.TokenValidationParameters = parameters;
            });

        services.AddAuthorization();
    }

    private static void AddDbContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
    }

    private static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<ExampleRepository>();
        services.AddScoped<IExampleRepository>(sp => sp.GetRequiredService<ExampleRepository>());
        services.AddScoped<IAddRepository<ExampleEntity>>(sp => sp.GetRequiredService<ExampleRepository>());
        services.AddScoped<IGetByIdRepository<ExampleEntity>>(sp => sp.GetRequiredService<ExampleRepository>());
        services.AddScoped<IUpdateRepository<ExampleEntity>>(sp => sp.GetRequiredService<ExampleRepository>());
        services.AddScoped<IDeleteRepository<ExampleEntity>>(sp => sp.GetRequiredService<ExampleRepository>());
    }
}
