using System.Net.Http.Json;
using FluentAssertions;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
using TIKR.Shared.TestFixtures;
using TIKR.Api.Tests.Fixtures;

namespace TIKR.Api.Tests.Endpoints;

[Trait("Category", TestCategories.FullyTested)]
public class DocumentAgentEndpointTests : IClassFixture<TikrWebApplicationFactory>
{
    private static readonly string FixtureDir = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "fixtures", "agent-scan"));

    private readonly HttpClient _client;

    public DocumentAgentEndpointTests(TikrWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task AgentScan_ReturnsStubExtractionForUpload()
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent("pdf bytes"u8.ToArray()), "file", "audit-report.pdf");

        var response = await _client.PostAsync("/api/ai/agent-scan", content);

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadFromJsonAsync<DocumentAgentResult>();
        body!.SuggestedCategory.Should().Be(RequirementCategory.Audit);
        body.ProcessedLocally.Should().BeTrue();
        body.TablesExtractedCount.Should().Be(3);
        body.UsedSyncfusionTools.Should().BeFalse();
    }

    [Fact]
    public async Task AgentScan_ExtractsTxtFixture()
    {
        var fixturePath = Path.Combine(FixtureDir, "wiley-periodic-report.txt");
        File.Exists(fixturePath).Should().BeTrue("fixture must be committed under tests/fixtures/agent-scan/");

        var bytes = await File.ReadAllBytesAsync(fixturePath);
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(bytes), "file", "wiley-periodic-report.txt");

        var response = await _client.PostAsync("/api/ai/agent-scan", content);

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadFromJsonAsync<DocumentAgentResult>();
        body!.ExtractedText.Should().Contain("Wiley periodic report due Q1 2026");
        body.StoragePath.Should().StartWith("agent-scans/");
        body.UsedSyncfusionTools.Should().BeFalse();
    }
}
