using System.Net.Http.Json;
using FluentAssertions;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
using TIKR.Api.Tests.Fixtures;

namespace TIKR.Api.Tests.Endpoints;

public class DocumentAgentEndpointTests : IClassFixture<TikrWebApplicationFactory>
{
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
    }
}
