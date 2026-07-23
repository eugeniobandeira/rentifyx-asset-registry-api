using System.Security.Cryptography;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RentifyxAssetRegistry.Domain.Interfaces.Media;
using RentifyxAssetRegistry.Infrastructure.Configuration;
using RentifyxAssetRegistry.Infrastructure.Constants;
using RentifyxAssetRegistry.Infrastructure.Storage;

namespace RentifyxAssetRegistry.IoC;

internal static class InfrastructureDependencyInjection
{
    internal static IServiceCollection Register(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddJwtAuthentication(configuration);
        services.AddMediaStorage(configuration);

        return services;
    }

    private static void AddMediaStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MediaStorageOptions>(options =>
        {
            options.BucketName = configuration[ConfigurationKeys.AwsS3BucketName] ?? string.Empty;

            string? expirySeconds = configuration[ConfigurationKeys.AwsS3PresignedUrlExpirySeconds];

            if (int.TryParse(expirySeconds, out int parsedExpirySeconds))
            {
                options.PresignedUrlExpirySeconds = parsedExpirySeconds;
            }
        });

        services.AddSingleton<IAmazonS3>(_ =>
        {
            string? serviceUrl = configuration[ConfigurationKeys.AwsS3ServiceUrl];

            AmazonS3Config s3Config = new();

            if (!string.IsNullOrEmpty(serviceUrl))
            {
                s3Config.ServiceURL = serviceUrl;
                s3Config.ForcePathStyle = true;
            }
            else
            {
                s3Config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(
                    configuration[ConfigurationKeys.AwsRegion] ?? ConfigurationKeys.DefaultAwsRegion);
            }

            return new AmazonS3Client(s3Config);
        });

        services.AddScoped<IMediaStorageService, S3MediaStorageService>();
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
