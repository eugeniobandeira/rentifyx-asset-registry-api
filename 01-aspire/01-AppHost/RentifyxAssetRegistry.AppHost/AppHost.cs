using RentifyxAssetRegistry.AppHost;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> postgresPassword = builder.AddParameter(
    "postgres-password",
    value: "postgres",
    secret: true);

IResourceBuilder<PostgresDatabaseResource> database = builder
    .AddPostgres("postgres", password: postgresPassword)
    .WithDataVolume()
    .AddDatabase("database");

builder.AddProject<Projects.RentifyxAssetRegistry_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpHealthCheck("/health")
    .WithScalar();

await builder.Build().RunAsync();
