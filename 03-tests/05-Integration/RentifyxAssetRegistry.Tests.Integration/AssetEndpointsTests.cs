using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RentifyxAssetRegistry.Application.Features.Assets;
using RentifyxAssetRegistry.Application.Features.Assets.Handlers.Create.Request;
using RentifyxAssetRegistry.Domain.Enums;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Integration;

public sealed class AssetEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AssetEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static CreateAssetRequest BuildCreateRequest(Guid ownerId) => new(
        ownerId,
        "Excavator CAT 320",
        "Heavy duty excavator available for rent.",
        1000m,
        Guid.NewGuid(),
        Guid.NewGuid().ToString());

    private async Task<CreateAssetResponse> CreateAssetAsync(Guid ownerId)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/assets", BuildCreateRequest(ownerId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateAssetResponse>())!;
    }

    [Fact]
    public async Task CreateAsset_ValidRequest_Returns201WithLocation()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/assets", BuildCreateRequest(Guid.NewGuid()));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        CreateAssetResponse? body = await response.Content.ReadFromJsonAsync<CreateAssetResponse>();
        body!.Status.Should().Be(AssetStatus.Draft);
    }

    [Fact]
    public async Task CreateAsset_SuspendedOwner_Returns403()
    {
        Guid ownerId = Guid.NewGuid();
        _factory.OwnerStatusValidator.MarkSuspended(ownerId);

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/assets", BuildCreateRequest(ownerId));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateAsset_BlankTitle_Returns422()
    {
        var invalidRequest = new
        {
            OwnerId = Guid.NewGuid(),
            Title = "",
            Description = "Heavy duty excavator available for rent.",
            Price = 1000m,
            CategoryId = Guid.NewGuid(),
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/assets", invalidRequest);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetAssetById_ExistingAsset_Returns200()
    {
        CreateAssetResponse created = await CreateAssetAsync(Guid.NewGuid());

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/assets/{created.AssetId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GetAssetByIdResponse? body = await response.Content.ReadFromJsonAsync<GetAssetByIdResponse>();
        body!.Id.Should().Be(created.AssetId);
    }

    [Fact]
    public async Task GetAssetById_MissingAsset_Returns404()
    {
        HttpResponseMessage response = await _client.GetAsync($"/api/v1/assets/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RequestMediaUpload_ValidRequest_ReturnsPresignedUrl()
    {
        Guid ownerId = Guid.NewGuid();
        CreateAssetResponse created = await CreateAssetAsync(ownerId);

        var body = new { OwnerId = ownerId, MimeType = "image/png", SizeBytes = 1024L };

        HttpResponseMessage response = await _client.PostAsJsonAsync($"/api/v1/assets/{created.AssetId}/media/upload-request", body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        RequestMediaUploadResponse? result = await response.Content.ReadFromJsonAsync<RequestMediaUploadResponse>();
        result!.S3Key.Should().Contain(created.AssetId.ToString());
    }

    [Fact]
    public async Task ConfirmMediaUpload_ValidRequest_ReturnsConfirmation()
    {
        Guid ownerId = Guid.NewGuid();
        CreateAssetResponse created = await CreateAssetAsync(ownerId);

        var uploadBody = new { OwnerId = ownerId, MimeType = "image/png", SizeBytes = 1024L };
        HttpResponseMessage uploadResponse = await _client.PostAsJsonAsync($"/api/v1/assets/{created.AssetId}/media/upload-request", uploadBody);
        RequestMediaUploadResponse uploadResult = (await uploadResponse.Content.ReadFromJsonAsync<RequestMediaUploadResponse>())!;

        var confirmBody = new { OwnerId = ownerId, S3Key = uploadResult.S3Key, MimeType = "image/png", SizeBytes = 1024L };
        HttpResponseMessage response = await _client.PostAsJsonAsync($"/api/v1/assets/{created.AssetId}/media/confirm", confirmBody);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SubmitForModerationThenAdminReview_ApprovedFlow_TransitionsAssetToActiveAndAppearsInSearch()
    {
        Guid ownerId = Guid.NewGuid();
        CreateAssetResponse created = await CreateAssetAsync(ownerId);

        HttpResponseMessage submitResponse = await _client.PostAsJsonAsync(
            $"/api/v1/assets/{created.AssetId}/submit-for-moderation", new { OwnerId = ownerId });
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage reviewResponse = await _client.PostAsJsonAsync(
            $"/api/v1/assets/{created.AssetId}/admin-review", new { Approve = true, IsAdmin = true, Reason = (string?)null });
        reviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        AssetModerationResponse? reviewBody = await reviewResponse.Content.ReadFromJsonAsync<AssetModerationResponse>();
        reviewBody!.Status.Should().Be(AssetStatus.Active);

        HttpResponseMessage searchResponse = await _client.GetAsync("/api/v1/assets?pageSize=10");
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        SearchAssetsResponse? searchBody = await searchResponse.Content.ReadFromJsonAsync<SearchAssetsResponse>();
        searchBody!.Items.Should().Contain(i => i.Id == created.AssetId);
    }
}
