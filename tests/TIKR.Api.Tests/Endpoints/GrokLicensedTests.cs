using System.Net.Http.Json;
using FluentAssertions;
using TIKR.Api.Tests.Fixtures;
using TIKR.Shared.DTOs;

namespace TIKR.Api.Tests.Endpoints;

/// <summary>
/// Live xAI Grok integration tests. Skips when <c>GROK_API_KEY</c> / <c>XAI_API_KEY</c> is unset (default CI).
/// </summary>
[Trait("Category", "GrokLicensed")]
public class GrokLicensedTests : IClassFixture<GrokAgentWebApplicationFactory>
{
    private readonly HttpClient _client;

    public GrokLicensedTests(GrokAgentWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task AskAdvanced_ReturnsGrokResponse_WhenApiKeyConfigured()
    {
        if (!GrokAgentWebApplicationFactory.IsConfigured)
            return;

        var response = await _client.PostAsJsonAsync(
            "/api/ai/ask-advanced",
            new AskAdvancedRequest("Reply with the phrase TIKR-GROK-OK and nothing else.", null));

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadFromJsonAsync<AskAdvancedResponse>();
        body!.UsedGrok.Should().BeTrue();
        body.Answer.Should().NotContain("Unable to get a response from Grok");
        body.Answer!.Length.Should().BeGreaterThan(3);
    }

    [Fact]
    public async Task AiStatus_ReportsGrokEnabled_WhenApiKeyConfigured()
    {
        if (!GrokAgentWebApplicationFactory.IsConfigured)
            return;

        var response = await _client.GetAsync("/api/ai/status");

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadFromJsonAsync<AiStatusResponse>();
        body!.GrokEnabled.Should().BeTrue();
    }
}
