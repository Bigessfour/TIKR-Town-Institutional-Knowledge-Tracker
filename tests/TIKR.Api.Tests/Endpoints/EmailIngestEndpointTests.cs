using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TIKR.Api.Tests.Fixtures;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;

namespace TIKR.Api.Tests.Endpoints;

public class EmailIngestEndpointTests : IClassFixture<EmailInboxWebApplicationFactory>
{
    private readonly EmailInboxWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EmailIngestEndpointTests(EmailInboxWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostEmailIngest_ElectionEml_CreatesContactSideEffects()
    {
        var drop = Path.Combine(_factory.InboxPath, "Update to election procedures.eml");
        await File.WriteAllTextAsync(drop, ElectionEml);

        var response = await _client.PostAsync("/api/email/ingest", content: null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<EmailIngestionResult>();
        result.Should().NotBeNull();
        result!.Ingested.Should().BeGreaterThanOrEqualTo(1);
        (result.ContactsCreated + result.ContactsUpdated).Should().BeGreaterThan(0);
        result.KnowledgeCreated.Should().BeGreaterThan(0);

        var contacts = await _client.GetFromJsonAsync<List<ContactDto>>("/api/contacts");
        contacts.Should().Contain(c =>
            c.Categories.HasFlag(ContactCategory.Election) &&
            ((c.Email != null && c.Email.Contains("county.example.gov")) ||
             c.Name.Contains("Jordan", StringComparison.OrdinalIgnoreCase) ||
             c.Name.Contains("County", StringComparison.OrdinalIgnoreCase)));

        var notices = await _client.GetFromJsonAsync<List<EmailExtractionNoticeDto>>("/api/email/notices");
        notices.Should().NotBeNull();
        notices!.Should().Contain(n => n.FileName.Contains("election", StringComparison.OrdinalIgnoreCase));
    }

    private const string ElectionEml =
        """
        From: "County Clerk Elections" <elections@county.example.gov>
        To: clerk@townofwiley.example.gov
        Subject: Update to election procedures — canvass packet
        Date: Mon, 14 Aug 2026 10:15:00 -0600
        Content-Type: text/plain; charset=UTF-8

        Contact: Jordan Lee
        Phone: 970-555-0142
        Email: jordan.lee@county.example.gov
        Submit to: County Clerk Elections Division
        Due date: 11/05/2026

        County Clerk SOS ballot canvass update.
        """;
}
