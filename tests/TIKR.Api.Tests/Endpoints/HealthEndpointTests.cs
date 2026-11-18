using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using TIKR.Api.Tests.Fixtures;

namespace TIKR.Api.Tests.Endpoints;

public class HealthEndpointTests : IClassFixture<TikrWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(TikrWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task GetHealth_ReturnsHealthyStatus()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>();
        payload!.Status.Should().Be("healthy");
        payload.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetHealth_IncludesHostedServiceTimestamps()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        doc.RootElement.GetProperty("status").GetString().Should().Be("healthy");
        doc.RootElement.TryGetProperty("recovery", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("library", out _).Should().BeTrue();
        doc.RootElement.TryGetProperty("email", out _).Should().BeTrue();
    }

    private sealed record HealthResponse(string Status, DateTime Timestamp);
}
