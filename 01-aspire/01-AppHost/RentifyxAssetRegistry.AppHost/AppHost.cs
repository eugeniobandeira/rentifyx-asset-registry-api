using RentifyxAssetRegistry.AppHost;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.RentifyxAssetRegistry_Api>("api")
    .WithHttpHealthCheck("/health")
    .WithScalar();

await builder.Build().RunAsync();
