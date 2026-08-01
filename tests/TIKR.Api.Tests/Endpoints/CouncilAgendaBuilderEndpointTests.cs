using System.Net.Http.Json;
using FluentAssertions;
using TIKR.Api.Tests.Fixtures;
using TIKR.Shared.DTOs;

namespace TIKR.Api.Tests.Endpoints;

public class CouncilAgendaBuilderEndpointTests : IClassFixture<AiStubWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CouncilAgendaBuilderEndpointTests(AiStubWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task GetAgendaPreview_ReturnsDlgSections()
    {
        var response = await _client.GetAsync(
            "/api/council/agenda-builder/preview?meetingDate=2026-08-10&board=TOW");

        response.IsSuccessStatusCode.Should().BeTrue();
        var preview = await response.Content.ReadFromJsonAsync<CouncilAgendaBuilderPreview>();
        preview.Should().NotBeNull();
        preview!.MeetingDate.Should().Be(new DateOnly(2026, 8, 10));
        preview.Sections.Should().Contain(s => s.SectionKey == "call_to_order");
        preview.Sections.Should().Contain(s => s.SectionKey == "adjourn");
    }

    [Fact]
    public async Task PostUnfinishedBusiness_ReturnsOkList()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/council/agenda-builder/unfinished-business",
            new UnfinishedBusinessRequest(new DateOnly(2026, 8, 10), "TOW"));

        response.IsSuccessStatusCode.Should().BeTrue();
        var body = await response.Content.ReadFromJsonAsync<List<UnfinishedBusinessSuggestion>>();
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMinutesPreview_ReturnsAgendaLinesAndDraftRequirement()
    {
        var response = await _client.GetAsync(
            "/api/council/minutes-builder/preview?meetingDate=2026-08-10&board=TOW");

        response.IsSuccessStatusCode.Should().BeTrue();
        var preview = await response.Content.ReadFromJsonAsync<CouncilMinutesBuilderPreview>();
        preview.Should().NotBeNull();
        preview!.MeetingDate.Should().Be(new DateOnly(2026, 8, 10));
        preview.AgendaLines.Should().NotBeEmpty();
    }
}
