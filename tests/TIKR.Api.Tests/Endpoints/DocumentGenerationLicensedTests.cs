using System.Net.Http.Json;
using FluentAssertions;
using TIKR.Api.Tests.Fixtures;
using TIKR.Shared.DTOs;

namespace TIKR.Api.Tests.Endpoints;

[Trait("Category", "SyncfusionLicensed")]
public class DocumentGenerationLicensedTests : IClassFixture<SyncfusionAgentWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DocumentGenerationLicensedTests(SyncfusionAgentWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task GenerateCouncilAgenda_WhenLicensed_ReturnsPdf()
    {
        if (!SyncfusionAgentWebApplicationFactory.IsLicensed)
            return;

        var response = await _client.PostAsJsonAsync(
            "/api/documents/generate/council-agenda",
            new CouncilAgendaRequest("Wiley", new DateOnly(2026, 7, 8), []));

        response.IsSuccessStatusCode.Should().BeTrue();
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Take(4).Should().Equal([(byte)'%', (byte)'P', (byte)'D', (byte)'F']);
    }
}
