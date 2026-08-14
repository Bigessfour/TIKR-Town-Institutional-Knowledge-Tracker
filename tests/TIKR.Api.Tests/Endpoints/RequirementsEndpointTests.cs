using System.Net;
using System.Net.Http.Headers;
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

    // Exercises thin endpoints delegating to services (via Program.cs):
    // Covers RequirementService.CreateAsync, UpdateAsync, DeleteAsync, LinkDocumentAsync, UnlinkDocumentAsync
    // + CouncilPacketEndpoints helpers (LoadRequirementLinksAsync, MapRequirement, BuildCouncilPacketRequirementsAsync)

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
        // Covers RequirementService.CreateAsync via thin /api/requirements POST endpoint + audit
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
    public async Task PostRequirement_PersistsDueOutContactFields()
    {
        var request = new CreateRequirementRequest(
            "Liquor license renewal",
            "Annual filing",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            RecurrenceType.Annual,
            RequirementCategory.Custom,
            SubmitTo: "Colorado Department of Revenue",
            ContactName: "Jane Smith",
            ContactEmail: "jane@example.com",
            ContactPhone: "303-555-0100");

        var response = await _client.PostAsJsonAsync("/api/requirements", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await response.Content.ReadFromJsonAsync<RequirementDto>();
        created!.SubmitTo.Should().Be("Colorado Department of Revenue");
        created.ContactName.Should().Be("Jane Smith");
        created.ContactEmail.Should().Be("jane@example.com");
        created.ContactPhone.Should().Be("303-555-0100");

        var fetched = await _client.GetFromJsonAsync<RequirementDto>($"/api/requirements/{created.Id}");
        fetched!.ContactName.Should().Be("Jane Smith");
        fetched.SubmitTo.Should().Be("Colorado Department of Revenue");
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
        // Covers RequirementService.UpdateAsync via thin /api/requirements PUT endpoint + audit
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
        // Covers RequirementService.DeleteAsync via thin /api/requirements DELETE endpoint
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

    [Fact]
    public async Task DeleteRequirement_ReturnsNotFoundForMissingId()
    {
        var response = await _client.DeleteAsync($"/api/requirements/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostLinkDocumentToRequirement_CreatesLinkAndAuditsLink()
    {
        // Covers RequirementService.LinkDocumentAsync via thin POST /api/requirements/{id}/documents + audit "Link"
        var createReq = await _client.PostAsJsonAsync("/api/requirements", new CreateRequirementRequest(
            "Linkable Task",
            "For doc link test",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
            RecurrenceType.None,
            RequirementCategory.Custom));
        var req = await createReq.Content.ReadFromJsonAsync<RequirementDto>();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("link test doc"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "link-test.txt");
        var createDoc = await _client.PostAsync("/api/documents", content);
        var doc = await createDoc.Content.ReadFromJsonAsync<DocumentDto>();

        var linkResp = await _client.PostAsJsonAsync($"/api/requirements/{req!.Id}/documents", new LinkRequirementDocumentRequest(doc!.Id));
        linkResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var audit = await _client.GetFromJsonAsync<List<AuditLogDto>>("/api/audit?limit=10");
        audit.Should().Contain(a => a.Action == "Link" && a.EntityType == "Requirement");
    }

    [Fact]
    public async Task GetDocumentRequirements_ReturnsLinksOrderedByDueDate()
    {
        // Proof: GET /api/documents/{id}/requirements (EF OrderBy on entity DueDate, not DTO)
        var earlier = await (await _client.PostAsJsonAsync("/api/requirements", new CreateRequirementRequest(
            "Earlier linked due-out",
            null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            RecurrenceType.None,
            RequirementCategory.Custom))).Content.ReadFromJsonAsync<RequirementDto>();
        var later = await (await _client.PostAsJsonAsync("/api/requirements", new CreateRequirementRequest(
            "Later linked due-out",
            null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)),
            RecurrenceType.None,
            RequirementCategory.Custom))).Content.ReadFromJsonAsync<RequirementDto>();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("doc-req-links"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "doc-req-links.txt");
        var doc = await (await _client.PostAsync("/api/documents", content)).Content.ReadFromJsonAsync<DocumentDto>();

        (await _client.PostAsJsonAsync($"/api/requirements/{later!.Id}/documents", new LinkRequirementDocumentRequest(doc!.Id)))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.PostAsJsonAsync($"/api/requirements/{earlier!.Id}/documents", new LinkRequirementDocumentRequest(doc.Id)))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await _client.GetAsync($"/api/documents/{doc.Id}/requirements");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var links = await response.Content.ReadFromJsonAsync<List<DocumentRequirementLinkDto>>();
        links.Should().NotBeNull();
        links!.Select(l => l.RequirementId).Should().ContainInOrder(earlier.Id, later.Id);
        links.Select(l => l.Title).Should().ContainInOrder("Earlier linked due-out", "Later linked due-out");
    }

    [Fact]
    public async Task GetDocumentRequirements_ReturnsNotFoundForMissingDocument()
    {
        var response = await _client.GetAsync($"/api/documents/{Guid.NewGuid()}/requirements");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteLinkDocumentFromRequirement_RemovesLinkAndAuditsUnlink()
    {
        // Covers RequirementService.UnlinkDocumentAsync via thin DELETE /api/requirements/{id}/documents/{docId} + audit "Unlink"
        var createReq = await _client.PostAsJsonAsync("/api/requirements", new CreateRequirementRequest(
            "Unlinkable Task",
            null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            RecurrenceType.None,
            RequirementCategory.Custom));
        var req = await createReq.Content.ReadFromJsonAsync<RequirementDto>();

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent("unlink test"u8.ToArray());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "file", "unlink-test.txt");
        var createDoc = await _client.PostAsync("/api/documents", content);
        var doc = await createDoc.Content.ReadFromJsonAsync<DocumentDto>();

        await _client.PostAsJsonAsync($"/api/requirements/{req!.Id}/documents", new LinkRequirementDocumentRequest(doc!.Id));

        var delResp = await _client.DeleteAsync($"/api/requirements/{req.Id}/documents/{doc.Id}");
        delResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var audit = await _client.GetFromJsonAsync<List<AuditLogDto>>("/api/audit?limit=10");
        audit.Should().Contain(a => a.Action == "Unlink" && a.EntityType == "Requirement");
    }

    [Fact]
    public async Task Checklist_CrudCompleteAndReportsProgress()
    {
        var create = await _client.PostAsJsonAsync("/api/requirements", new CreateRequirementRequest(
            "Checklist host",
            "Playbook test",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            RecurrenceType.None,
            RequirementCategory.Custom));
        var req = await create.Content.ReadFromJsonAsync<RequirementDto>();

        var add = await _client.PostAsJsonAsync(
            $"/api/requirements/{req!.Id}/checklist",
            new CreateRequirementChecklistItemRequest("Post notice", DueOffsetDays: 10));
        add.StatusCode.Should().Be(HttpStatusCode.Created);
        var item = await add.Content.ReadFromJsonAsync<RequirementChecklistItemDto>();
        item!.Title.Should().Be("Post notice");
        item.EffectiveDueDate.Should().Be(req.DueDate.AddDays(-10));

        var list = await _client.GetFromJsonAsync<List<RequirementChecklistItemDto>>(
            $"/api/requirements/{req.Id}/checklist");
        list.Should().ContainSingle(i => i.Id == item.Id);

        var complete = await _client.PostAsJsonAsync(
            $"/api/requirements/{req.Id}/checklist/{item.Id}/complete",
            new CompleteRequirementChecklistItemRequest(true));
        complete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var fetched = await _client.GetFromJsonAsync<RequirementDto>($"/api/requirements/{req.Id}");
        fetched!.ChecklistTotal.Should().Be(1);
        fetched.ChecklistCompleted.Should().Be(1);

        var audit = await _client.GetFromJsonAsync<List<AuditLogDto>>("/api/audit?limit=15");
        audit.Should().Contain(a => a.Action == "ChecklistCreate" && a.EntityType == "Requirement");
        audit.Should().Contain(a => a.Action == "ChecklistComplete" && a.EntityType == "Requirement");

        var del = await _client.DeleteAsync($"/api/requirements/{req.Id}/checklist/{item.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetFromJsonAsync<List<RequirementChecklistItemDto>>(
            $"/api/requirements/{req.Id}/checklist")).Should().BeEmpty();
    }

    [Fact]
    public async Task PutChecklistItem_UpdatesFieldsAndAudits()
    {
        var create = await _client.PostAsJsonAsync("/api/requirements", new CreateRequirementRequest(
            "Checklist update host",
            null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(45)),
            RecurrenceType.None,
            RequirementCategory.Custom));
        var req = await create.Content.ReadFromJsonAsync<RequirementDto>();

        var add = await _client.PostAsJsonAsync(
            $"/api/requirements/{req!.Id}/checklist",
            new CreateRequirementChecklistItemRequest("Draft step", DueOffsetDays: 14));
        var item = await add.Content.ReadFromJsonAsync<RequirementChecklistItemDto>();

        var put = await _client.PutAsJsonAsync(
            $"/api/requirements/{req.Id}/checklist/{item!.Id}",
            new UpdateRequirementChecklistItemRequest(
                "Revised step",
                Description: "Updated playbook note",
                IsRequired: true,
                IsCompleted: false,
                DueOffsetDays: 7,
                SortOrder: item.SortOrder,
                SubmitTo: "County Clerk"));
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await put.Content.ReadFromJsonAsync<RequirementChecklistItemDto>();
        updated!.Title.Should().Be("Revised step");
        updated.Description.Should().Be("Updated playbook note");
        updated.DueOffsetDays.Should().Be(7);
        updated.SubmitTo.Should().Be("County Clerk");
        updated.EffectiveDueDate.Should().Be(req.DueDate.AddDays(-7));

        var audit = await _client.GetFromJsonAsync<List<AuditLogDto>>("/api/audit?limit=15");
        audit.Should().Contain(a => a.Action == "ChecklistUpdate" && a.EntityType == "Requirement");
    }

    [Fact]
    public async Task PutChecklistReorder_ReordersItems()
    {
        var create = await _client.PostAsJsonAsync("/api/requirements", new CreateRequirementRequest(
            "Checklist reorder host",
            null,
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20)),
            RecurrenceType.None,
            RequirementCategory.Election));
        var req = await create.Content.ReadFromJsonAsync<RequirementDto>();

        var a = await (await _client.PostAsJsonAsync(
            $"/api/requirements/{req!.Id}/checklist",
            new CreateRequirementChecklistItemRequest("First", DueOffsetDays: 5)))
            .Content.ReadFromJsonAsync<RequirementChecklistItemDto>();
        var b = await (await _client.PostAsJsonAsync(
            $"/api/requirements/{req.Id}/checklist",
            new CreateRequirementChecklistItemRequest("Second", DueOffsetDays: 1)))
            .Content.ReadFromJsonAsync<RequirementChecklistItemDto>();

        var reorder = await _client.PutAsJsonAsync(
            $"/api/requirements/{req.Id}/checklist/reorder",
            new ReorderRequirementChecklistRequest([b!.Id, a!.Id]));
        reorder.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await _client.GetFromJsonAsync<List<RequirementChecklistItemDto>>(
            $"/api/requirements/{req.Id}/checklist");
        list!.Select(i => i.Id).Should().ContainInOrder(b.Id, a.Id);

        var audit = await _client.GetFromJsonAsync<List<AuditLogDto>>("/api/audit?limit=20");
        audit.Should().Contain(row => row.Action == "ChecklistReorder" && row.EntityType == "Requirement");
    }

    [Fact]
    public async Task GetRequirements_ElectionCanvass_IncludesSeededChecklistProgress()
    {
        var items = await _client.GetFromJsonAsync<List<RequirementDto>>("/api/requirements");
        var canvass = items!.First(r => r.Title.Contains("Election Canvass"));
        canvass.ChecklistTotal.Should().BeGreaterThanOrEqualTo(5);

        var checklist = await _client.GetFromJsonAsync<List<RequirementChecklistItemDto>>(
            $"/api/requirements/{canvass.Id}/checklist");
        checklist.Should().Contain(i => i.Title.Contains("canvass packet", StringComparison.OrdinalIgnoreCase));
    }

    private sealed record AuditLogDto(
        Guid Id,
        string Action,
        string EntityType,
        Guid? EntityId,
        string? Details,
        DateTime Timestamp);
}
