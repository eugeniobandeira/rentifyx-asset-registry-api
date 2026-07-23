using RentifyxAssetRegistry.AppHost;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<KafkaServerResource> kafka = builder.AddKafka("kafka");

builder.AddProject<Projects.RentifyxAssetRegistry_Api>("api")
    .WithReference(kafka)
    .WithHttpHealthCheck("/health")
    .WithScalar();

await builder.Build().RunAsync();
