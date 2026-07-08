using System.Net.Http.Json;
using FluentAssertions;
using TIKR.Api.Tests.Fixtures;
using TIKR.Shared.DTOs;

namespace TIKR.Api.Tests.Endpoints;

public class DocumentSdkStatusEndpointTests : IClassFixture<TikrWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DocumentSdkStatusEndpointTests(TikrWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task GetDocumentSdkStatus_ReturnsPayload()
    {
        var status = await _client.GetFromJsonAsync<DocumentSdkStatusDto>("/api/system/document-sdk-status");

        status.Should().NotBeNull();
        status!.AgentToolsEnabled.Should().BeFalse();
        status.OrchestrationEnabled.Should().BeFalse();
    }
}
