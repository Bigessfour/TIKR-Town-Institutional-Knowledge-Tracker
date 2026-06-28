using System.Net.Http.Json;
using FluentAssertions;
using TIKR.Api.Tests.Fixtures;
using TIKR.Shared.DTOs;

namespace TIKR.Api.Tests.Endpoints;

/// <summary>
/// Licensed Syncfusion agent-scan integration tests. Skips when <c>SYNCFUSION_LICENSE_KEY</c> is unset (default CI).
/// Run locally or in <c>TIKR Syncfusion Agent Smoke</c> workflow with the repo secret set.
/// </summary>
[Trait("Category", "SyncfusionLicensed")]
public class DocumentAgentSyncfusionLicensedTests : IClassFixture<SyncfusionAgentWebApplicationFactory>
{
    private static readonly string FixtureDir = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "fixtures", "agent-scan"));

    private readonly HttpClient _client;

    public DocumentAgentSyncfusionLicensedTests(SyncfusionAgentWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task AgentScan_ExtractsPdfFixture_WhenLicensed()
    {
        if (!SyncfusionAgentWebApplicationFactory.IsLicensed)
            return;

        var bytes = await File.ReadAllBytesAsync(Path.Combine(FixtureDir, "minimal-clerk-report.pdf"));
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(bytes), "file", "minimal-clerk-report.pdf");

        var response = await _client.PostAsync("/api/ai/agent-scan", content);

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadFromJsonAsync<DocumentAgentResult>();
        body!.UsedSyncfusionTools.Should().BeTrue();
        body.ExtractedText.Should().Contain("Wiley clerk report");
    }

    [Fact]
    public async Task AgentScan_ExtractsDocxFixture_WhenLicensed()
    {
        if (!SyncfusionAgentWebApplicationFactory.IsLicensed)
            return;

        var bytes = await File.ReadAllBytesAsync(Path.Combine(FixtureDir, "clerk-memo.docx"));
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(bytes), "file", "clerk-memo.docx");

        var response = await _client.PostAsync("/api/ai/agent-scan", content);

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadFromJsonAsync<DocumentAgentResult>();
        body!.UsedSyncfusionTools.Should().BeTrue();
        body.ExtractedText!.ToLowerInvariant().Should().Contain("wiley");
    }
}
