using FluentAssertions;
using TIKR.Infrastructure.Services;
using TIKR.Shared.DTOs;
using TIKR.Shared.Helpers;
using TIKR.Shared.TestFixtures;

namespace TIKR.Infrastructure.Tests.Services;

[Trait("Category", TestCategories.FullyTested)]
public class DocumentIntakeScanServiceMergeTests
{
    [Fact]
    public void SeedFromHeuristic_MapsDueAndContact()
    {
        var parsed = DueOutFieldParser.Parse("""
            Liquor License Renewal
            Submit to: Colorado Department of Revenue
            Contact: Jane Smith
            Due date: 03/15/2027
            Email jane.smith@example.com
            Phone (303) 555-0199
            """);

        var payload = DocumentIntakeScanService.SeedFromHeuristic(parsed);

        payload.DueDates.Should().ContainSingle(d => d.Date == new DateOnly(2027, 3, 15));
        payload.Contacts.Should().ContainSingle(c =>
            c.Name.Contains("Jane", StringComparison.OrdinalIgnoreCase)
            && c.Email == "jane.smith@example.com");
    }

    [Fact]
    public void ParseModelJson_ReadsArrays()
    {
        const string json = """
            {
              "dueDates":[{"date":"2026-05-01","label":"File canvass","sourceSnippet":"due May 1"}],
              "contacts":[{"name":"Pat","organization":"County","email":"pat@co.example","phone":"719-555-0199"}],
              "actionItems":[{"title":"Post notice","dueDate":"2026-04-20","notes":"bulletin board"}]
            }
            """;

        var payload = DocumentIntakeScanService.ParseModelJson(json);

        payload.Should().NotBeNull();
        payload!.DueDates.Should().ContainSingle(d => d.Date == new DateOnly(2026, 5, 1));
        payload.Contacts.Should().ContainSingle(c => c.Name == "Pat" && c.Email == "pat@co.example");
        payload.ActionItems.Should().ContainSingle(a => a.Title == "Post notice" && a.DueDate == new DateOnly(2026, 4, 20));
    }

    [Fact]
    public void MergePayloadsPreferringLocalContacts_KeepsLocalEmailOverPlaceholder()
    {
        var local = new DocumentIntakeBriefPayload(
            [new DocumentIntakeDueDateDto(new DateOnly(2026, 6, 1), "Local due")],
            [new DocumentIntakeContactDto("Alex", "Town", "alex@town.example", "719-555-0111")],
            []);
        var cloud = new DocumentIntakeBriefPayload(
            [new DocumentIntakeDueDateDto(new DateOnly(2026, 6, 2), "Cloud due")],
            [new DocumentIntakeContactDto("Alex", "Town Hall", "[email]", "[phone]")],
            [new DocumentIntakeActionItemDto("Call county", new DateOnly(2026, 6, 1))]);

        var merged = DocumentIntakeScanService.MergePayloadsPreferringLocalContacts(local, cloud);

        merged.DueDates.Should().HaveCount(2);
        merged.Contacts.Should().ContainSingle(c =>
            c.Name == "Alex"
            && c.Email == "alex@town.example"
            && c.Phone == "719-555-0111");
        merged.ActionItems.Should().ContainSingle(a => a.Title == "Call county");
    }
}
