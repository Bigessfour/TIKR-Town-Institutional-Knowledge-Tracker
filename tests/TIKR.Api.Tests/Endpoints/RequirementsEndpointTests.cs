using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TIKR.Api.Tests.Fixtures;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;

namespace TIKR.Api.Tests.Endpoints;

public class RequirementsEndpointTests : IClassFixture<TikrWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RequirementsEndpointTests(TikrWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task GetRequirements_ReturnsSeededColoradoDeadlines()
    {
        var items = await _client.GetFromJsonAsync<List<RequirementDto>>("/api/requirements");
        items.Should().NotBeNull();
        items!.Count.Should().BeGreaterThanOrEqualTo(7);
        items.Should().Contain(r => r.Title.Contains("Budget"));
    }

    [Fact]
    public async Task PostRequirement_CreatesAndAudits()
    {
        var request = new CreateRequirementRequest(
            "Custom Clerk Task",
            "Test description",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            RecurrenceType.None,
            RequirementCategory.Custom);

        var response = await _client.PostAsJsonAsync("/api/requirements", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<RequirementDto>();
        created!.Title.Should().Be("Custom Clerk Task");

        var audit = await _client.GetFromJsonAsync<List<AuditLogDto>>("/api/audit?limit=5");
        audit.Should().Contain(a => a.Action == "Create" && a.EntityType == "Requirement");
    }

    [Fact]
    public async Task DeleteSystemSeededRequirement_ReturnsBadRequest()
    {
        var items = await _client.GetFromJsonAsync<List<RequirementDto>>("/api/requirements");
        var seeded = items!.First(r => r.IsSystemSeeded);

        var response = await _client.DeleteAsync($"/api/requirements/{seeded.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetRequirementById_ReturnsSingleItem()
    {
        var items = await _client.GetFromJsonAsync<List<RequirementDto>>("/api/requirements");
        var target = items!.First();

        var item = await _client.GetFromJsonAsync<RequirementDto>($"/api/requirements/{target.Id}");
        item.Should().NotBeNull();
        item!.Id.Should().Be(target.Id);
        item.Title.Should().Be(target.Title);
    }

    [Fact]
    public async Task GetRequirementById_ReturnsNotFoundForMissingId()
    {
        var response = await _client.GetAsync($"/api/requirements/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutRequirement_UpdatesAndAudits()
    {
        var create = await _client.PostAsJsonAsync("/api/requirements", new CreateRequirementRequest(
            "Editable task",
            "Original",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
            RecurrenceType.Monthly,
            RequirementCategory.Custom));

        var created = await create.Content.ReadFromJsonAsync<RequirementDto>();

        var update = new UpdateRequirementRequest(
            "Updated task",
            "Revised description",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)),
            RecurrenceType.Quarterly,
            RequirementCategory.Budget,
            IsCompleted: true);

        var response = await _client.PutAsJsonAsync($"/api/requirements/{created!.Id}", update);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<RequirementDto>();
        updated!.Title.Should().Be("Updated task");
        updated.IsCompleted.Should().BeTrue();

        var audit = await _client.GetFromJsonAsync<List<AuditLogDto>>("/api/audit?limit=10");
        audit.Should().Contain(a => a.Action == "Update" && a.EntityId == created.Id);
    }

    [Fact]
    public async Task PutRequirement_ReturnsNotFoundForMissingId()
    {
        var update = new UpdateRequirementRequest(
            "Missing",
            null,
            DateOnly.FromDateTime(DateTime.UtcNow),
            RecurrenceType.None,
            RequirementCategory.Custom,
            IsCompleted: false);

        var response = await _client.PutAsJsonAsync($"/api/requirements/{Guid.NewGuid()}", update);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCustomRequirement_ReturnsNoContent()
    {
        var request = new CreateRequirementRequest(
            "Temporary task",
            null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)),
            RecurrenceType.None,
            RequirementCategory.Custom);

        var create = await _client.PostAsJsonAsync("/api/requirements", request);
        var created = await create.Content.ReadFromJsonAsync<RequirementDto>();

        var response = await _client.DeleteAsync($"/api/requirements/{created!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private sealed record AuditLogDto(
        Guid Id,
        string Action,
        string EntityType,
        Guid? EntityId,
        string? Details,
        DateTime Timestamp);
}
