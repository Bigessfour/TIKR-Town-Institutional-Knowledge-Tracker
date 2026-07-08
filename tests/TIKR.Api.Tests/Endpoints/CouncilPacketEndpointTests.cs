using System.Net.Http.Json;
using FluentAssertions;
using TIKR.Api.Tests.Fixtures;
using TIKR.Shared.DTOs;

namespace TIKR.Api.Tests.Endpoints;

public class CouncilPacketEndpointTests : IClassFixture<TikrWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CouncilPacketEndpointTests(TikrWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task GenerateCouncilPacket_WithoutLicense_ReturnsServiceUnavailable()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/documents/generate/council-packet",
            new CreateCouncilPacketRequest("Wiley", DateOnly.FromDateTime(DateTime.UtcNow), null, []));

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.ServiceUnavailable);
    }
}

[Trait("Category", "SyncfusionLicensed")]
public class CouncilPacketLicensedTests : IClassFixture<SyncfusionAgentWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CouncilPacketLicensedTests(SyncfusionAgentWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task GenerateCouncilPacket_WhenLicensed_PersistsPdfAndDocx()
    {
        if (!SyncfusionAgentWebApplicationFactory.IsLicensed)
            return;

        var response = await _client.PostAsJsonAsync(
            "/api/documents/generate/council-packet",
            new CreateCouncilPacketRequest("Wiley", new DateOnly(2026, 7, 8), null, []));

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadFromJsonAsync<CouncilPacketResponse>();
        body.Should().NotBeNull();
        body!.Pdf.Should().NotBeNull();
        body.Docx.Should().NotBeNull();
        body.Pdf!.DownloadUrl.Should().Contain("/api/documents/");
        body.Docx!.DownloadUrl.Should().Contain("/api/documents/");

        var pdfResponse = await _client.GetAsync(body.Pdf.DownloadUrl);
        pdfResponse.IsSuccessStatusCode.Should().BeTrue();
        var pdfBytes = await pdfResponse.Content.ReadAsByteArrayAsync();
        pdfBytes.Take(4).Should().Equal([(byte)'%', (byte)'P', (byte)'D', (byte)'F']);
    }
}
