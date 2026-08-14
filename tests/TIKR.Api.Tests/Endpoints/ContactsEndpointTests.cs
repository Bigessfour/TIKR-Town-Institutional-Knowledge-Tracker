using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TIKR.Api.Tests.Fixtures;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;

namespace TIKR.Api.Tests.Endpoints;

public class ContactsEndpointTests : IClassFixture<TikrWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ContactsEndpointTests(TikrWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task GetContacts_ReturnsSeededElectionContacts()
    {
        var items = await _client.GetFromJsonAsync<List<ContactDto>>("/api/contacts");
        items.Should().NotBeNull();
        items!.Count.Should().BeGreaterThanOrEqualTo(3);
        items.Should().Contain(c => c.Name.Contains("County Clerk") && c.Categories.HasFlag(ContactCategory.Election));
    }

    [Fact]
    public async Task PostPutDeleteRestore_ContactCrudAndAudit()
    {
        var create = await _client.PostAsJsonAsync("/api/contacts", new CreateContactRequest(
            "Election – Test POC",
            Role: "Test Role",
            Organization: "Test Org",
            Address: "123 Main",
            Office: "Suite 1",
            Email: "poc@example.gov",
            Phone: "303-555-0199",
            Notes: "Seed test",
            Categories: ContactCategory.Election));

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<ContactDto>();
        created!.Name.Should().Be("Election – Test POC");

        var update = await _client.PutAsJsonAsync($"/api/contacts/{created.Id}", new UpdateContactRequest(
            "Election – Test POC Updated",
            Role: "Updated Role",
            Organization: created.Organization,
            Address: created.Address,
            Office: created.Office,
            Email: created.Email,
            Phone: created.Phone,
            Notes: created.Notes,
            Categories: ContactCategory.Election | ContactCategory.Compliance));
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await update.Content.ReadFromJsonAsync<ContactDto>();
        updated!.Name.Should().Be("Election – Test POC Updated");

        var del = await _client.DeleteAsync($"/api/contacts/{created.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var active = await _client.GetFromJsonAsync<List<ContactDto>>("/api/contacts");
        active!.Should().NotContain(c => c.Id == created.Id);

        var restore = await _client.PostAsync($"/api/contacts/{created.Id}/restore", content: null);
        restore.StatusCode.Should().Be(HttpStatusCode.OK);

        var audit = await _client.GetFromJsonAsync<List<AuditLogDto>>("/api/audit?limit=20");
        audit.Should().Contain(a => a.Action == "Create" && a.EntityType == "Contact");
        audit.Should().Contain(a => a.Action == "SoftDelete" && a.EntityType == "Contact");
        audit.Should().Contain(a => a.Action == "Restore" && a.EntityType == "Contact");
    }

    [Fact]
    public async Task LinkAndUnlink_RequirementContact_Audits()
    {
        var requirements = await _client.GetFromJsonAsync<List<RequirementDto>>("/api/requirements");
        var canvass = requirements!.First(r => r.Title.Contains("Election Canvass"));

        var create = await _client.PostAsJsonAsync("/api/contacts", new CreateContactRequest(
            "Canvass Alternate POC",
            Email: "alt@county.example.gov",
            Phone: "970-555-0198",
            Categories: ContactCategory.Election));
        var contact = await create.Content.ReadFromJsonAsync<ContactDto>();

        var link = await _client.PostAsync(
            $"/api/requirements/{canvass.Id}/contacts/{contact!.Id}?primary=true",
            content: null);
        link.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var linked = await _client.GetFromJsonAsync<List<ContactDto>>($"/api/requirements/{canvass.Id}/contacts");
        linked.Should().Contain(c => c.Id == contact.Id && c.IsPrimary == true);

        var req = await _client.GetFromJsonAsync<RequirementDto>($"/api/requirements/{canvass.Id}");
        req!.ContactName.Should().Be("Canvass Alternate POC");
        req.ContactEmail.Should().Be("alt@county.example.gov");

        var unlink = await _client.DeleteAsync($"/api/requirements/{canvass.Id}/contacts/{contact.Id}");
        unlink.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var audit = await _client.GetFromJsonAsync<List<AuditLogDto>>("/api/audit?limit=20");
        audit.Should().Contain(a => a.Action == "Link" && a.EntityType == "Requirement");
        audit.Should().Contain(a => a.Action == "Unlink" && a.EntityType == "Requirement");
    }

    private sealed record AuditLogDto(
        Guid Id,
        string Action,
        string EntityType,
        Guid? EntityId,
        string? Details,
        DateTime Timestamp);
}
