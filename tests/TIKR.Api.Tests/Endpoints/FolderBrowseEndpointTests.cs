using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TIKR.Api.Tests.Fixtures;
using TIKR.Shared.DTOs;
using TIKR.Shared.TestFixtures;

namespace TIKR.Api.Tests.Endpoints;

[Trait("Category", TestCategories.FullyTested)]
public class FolderBrowseEndpointTests : IClassFixture<TikrWebApplicationFactory>
{
    private readonly HttpClient _client;

    public FolderBrowseEndpointTests(TikrWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task BrowseFolders_Roots_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/settings/browse-folders");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FolderBrowseResultDto>();
        body.Should().NotBeNull();
        body!.Roots.Should().NotBeNull();
    }
}
