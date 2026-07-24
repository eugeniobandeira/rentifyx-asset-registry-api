using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RentifyxAssetRegistry.Api.Messaging;
using RentifyxAssetRegistry.Domain.Interfaces.Asset;
using RentifyxAssetRegistry.Domain.Interfaces.Category;
using RentifyxAssetRegistry.Domain.Interfaces.Media;
using RentifyxAssetRegistry.Tests.Integration.Fakes;

namespace RentifyxAssetRegistry.Tests.Integration;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public FakeOwnerStatusValidator OwnerStatusValidator { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAssetRepository>();
            services.AddSingleton<IAssetRepository, InMemoryAssetRepository>();

            services.RemoveAll<ICategoryRepository>();
            services.AddSingleton<ICategoryRepository, InMemoryCategoryRepository>();

            services.RemoveAll<IOwnerStatusValidator>();
            services.AddSingleton<IOwnerStatusValidator>(OwnerStatusValidator);

            services.RemoveAll<IMediaStorageService>();
            services.AddSingleton<IMediaStorageService, FakeMediaStorageService>();

            // Kafka consumers/producer need a real broker and aren't relevant to HTTP endpoint
            // tests -- removed so the test host doesn't spin their background loops.
            List<ServiceDescriptor> hostedServicesToRemove = services
                .Where(d => d.ServiceType == typeof(IHostedService)
                    && (d.ImplementationType == typeof(OutboxPublisher)
                        || d.ImplementationType == typeof(OwnerStatusConsumer)
                        || d.ImplementationType == typeof(ModerationVerdictConsumer)))
                .ToList();

            foreach (ServiceDescriptor descriptor in hostedServicesToRemove)
                services.Remove(descriptor);
        });
    }
}
