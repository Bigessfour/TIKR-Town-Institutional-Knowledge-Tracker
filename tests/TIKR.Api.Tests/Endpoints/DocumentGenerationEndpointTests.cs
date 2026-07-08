using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TIKR.Api.Tests.Fixtures;
using TIKR.Shared.DTOs;

namespace TIKR.Api.Tests.Endpoints;

public class DocumentGenerationEndpointTests : IClassFixture<TikrWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DocumentGenerationEndpointTests(TikrWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task GenerateCouncilAgenda_WithoutLicense_ReturnsServiceUnavailable()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/documents/generate/council-agenda",
            new CouncilAgendaRequest("Wiley", DateOnly.FromDateTime(DateTime.UtcNow), []));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task GenerateComplianceReport_WithoutLicense_ReturnsServiceUnavailable()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/documents/generate/compliance-report",
            new ComplianceReportRequest("Wiley", DateOnly.FromDateTime(DateTime.UtcNow), []));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }
}
