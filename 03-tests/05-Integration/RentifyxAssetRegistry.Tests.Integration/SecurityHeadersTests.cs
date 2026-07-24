using FluentAssertions;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Integration;

public sealed class SecurityHeadersTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task AnyResponse_IncludesSecurityHeaders()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/v1/categories");

        response.Headers.TryGetValues("X-Content-Type-Options", out IEnumerable<string>? contentTypeOptions);
        contentTypeOptions.Should().ContainSingle().Which.Should().Be("nosniff");

        response.Headers.TryGetValues("X-Frame-Options", out IEnumerable<string>? frameOptions);
        frameOptions.Should().ContainSingle().Which.Should().Be("DENY");

        response.Headers.TryGetValues("Referrer-Policy", out IEnumerable<string>? referrerPolicy);
        referrerPolicy.Should().ContainSingle().Which.Should().Be("strict-origin-when-cross-origin");

        // The Testing environment (see CustomWebApplicationFactory.ConfigureWebHost) is not
        // Development, so the strict CSP applies here the same as it would in Staging/Production.
        response.Headers.TryGetValues("Content-Security-Policy", out IEnumerable<string>? csp);
        csp.Should().ContainSingle().Which.Should().Be("default-src 'none'");
    }
}
