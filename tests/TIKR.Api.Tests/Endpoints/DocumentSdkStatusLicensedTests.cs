using System.Net.Http.Json;
using FluentAssertions;
using TIKR.Api.Tests.Fixtures;
using TIKR.Shared.DTOs;

namespace TIKR.Api.Tests.Endpoints;

[Trait("Category", "SyncfusionLicensed")]
public class DocumentSdkStatusLicensedTests : IClassFixture<SyncfusionAgentWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DocumentSdkStatusLicensedTests(SyncfusionAgentWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task GetDocumentSdkStatus_WhenLicensed_ReportsValidProbe()
    {
        if (!SyncfusionAgentWebApplicationFactory.IsLicensed)
            return;

        var status = await _client.GetFromJsonAsync<DocumentSdkStatusDto>("/api/system/document-sdk-status");

        status.Should().NotBeNull();
        status!.LicenseKeyConfigured.Should().BeTrue();
        status.LicenseProbePassed.Should().BeTrue();
        status.AgentToolsEnabled.Should().BeTrue();
    }
}
