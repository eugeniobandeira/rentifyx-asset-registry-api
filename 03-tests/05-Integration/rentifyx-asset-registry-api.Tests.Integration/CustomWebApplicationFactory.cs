using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace rentifyx_asset_registry_api.Tests.Integration;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
#pragma warning disable S1135
            // TODO: Replace infrastructure registrations with fakes/in-memory implementations.
#pragma warning restore S1135
        });
    }
}
