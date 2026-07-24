using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create.Request;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Integration;

public sealed class RequestSizeLimitTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private const long MaxRequestBodyBytes = 1024 * 1024;

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateAsset_BodyExceedsConfiguredLimit_Returns413()
    {
        CreateAssetRequest oversizedRequest = new(
            Guid.NewGuid(),
            "Excavator CAT 320",
            new string('a', (int)MaxRequestBodyBytes + 1024),
            1000m,
            Guid.NewGuid(),
            Guid.NewGuid().ToString());

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/assets", oversizedRequest);

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task CreateAsset_BodyWithinConfiguredLimit_IsNotRejectedBySizeLimiting()
    {
        using StringContent content = new(
            "{ invalid but small json }",
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage response = await _client.PostAsync("/api/v1/assets", content);

        response.StatusCode.Should().NotBe(HttpStatusCode.RequestEntityTooLarge);
    }
}
