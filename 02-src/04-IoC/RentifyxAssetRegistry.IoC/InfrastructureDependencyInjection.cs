using System.Security.Cryptography;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;
using RentifyxAssetRegistry.Domain.Interfaces.Category;
using RentifyxAssetRegistry.Infrastructure.Configuration;
using RentifyxAssetRegistry.Infrastructure.Constants;
using RentifyxAssetRegistry.Infrastructure.Persistence;

namespace RentifyxAssetRegistry.IoC;

internal static class InfrastructureDependencyInjection
{
    internal static IServiceCollection Register(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddJwtAuthentication(configuration);
        services.AddDynamoDb(configuration);

        return services;
    }

    private static void AddDynamoDb(this IServiceCollection services, IConfiguration configuration)
    {
        DynamoDbOptions options = new()
        {
            TableName = configuration[ConfigurationKeys.DynamoDbTableName] ?? "AssetRegistry",
            Region = configuration[ConfigurationKeys.DynamoDbRegion]
                ?? configuration[ConfigurationKeys.AwsRegion]
                ?? ConfigurationKeys.DefaultAwsRegion,
            ServiceUrl = configuration[ConfigurationKeys.DynamoDbServiceUrl]
        };

        services.AddSingleton(options);

        services.AddSingleton<IAmazonDynamoDB>(_ =>
        {
            AmazonDynamoDBConfig clientConfig = new()
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region)
            };

            if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
                clientConfig.ServiceURL = options.ServiceUrl;

            return new AmazonDynamoDBClient(clientConfig);
        });

        services.AddSingleton<IDynamoDBContext>(sp =>
            new DynamoDBContextBuilder()
                .WithDynamoDBClient(() => sp.GetRequiredService<IAmazonDynamoDB>())
                .Build());

        services.AddScoped<IAssetRepository, DynamoDbAssetRepository>();
        services.AddScoped<ICategoryRepository, DynamoDbCategoryRepository>();
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
}
