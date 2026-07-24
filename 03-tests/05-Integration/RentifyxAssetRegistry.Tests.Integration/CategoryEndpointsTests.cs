using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using RentifyxAssetRegistry.Application.Features.Categories;
using RentifyxAssetRegistry.Application.Features.Categories.Handlers.Create.Request;
using Xunit;

namespace RentifyxAssetRegistry.Tests.Integration;

public sealed class CategoryEndpointsTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateCategory_ValidRequest_Returns201WithLocation()
    {
        CreateCategoryRequest request = new("Heavy Equipment", null, true);

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/categories", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        CategoryResponse? body = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        body!.Name.Should().Be("Heavy Equipment");
    }

    [Fact]
    public async Task UpdateCategory_RenameLeafCategory_Returns200()
    {
        CreateCategoryRequest createRequest = new("Excavators", null, true);
        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/v1/categories", createRequest);
        CategoryResponse created = (await createResponse.Content.ReadFromJsonAsync<CategoryResponse>())!;

        var updateBody = new { IsAdmin = true, NewName = "Excavators & Loaders", NewParentCategoryId = (Guid?)null };
        HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/v1/categories/{created.Id}", updateBody);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CategoryResponse? body = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        body!.Name.Should().Be("Excavators & Loaders");
    }

    [Fact]
    public async Task ListCategories_ReturnsCreatedCategory()
    {
        CreateCategoryRequest createRequest = new("Cranes", null, true);
        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/v1/categories", createRequest);
        CategoryResponse created = (await createResponse.Content.ReadFromJsonAsync<CategoryResponse>())!;

        HttpResponseMessage response = await _client.GetAsync("/api/v1/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        IReadOnlyList<CategoryResponse>? body = await response.Content.ReadFromJsonAsync<IReadOnlyList<CategoryResponse>>();
        body!.Should().Contain(c => c.Id == created.Id);
    }
}
