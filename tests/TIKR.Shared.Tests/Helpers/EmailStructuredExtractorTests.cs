using FluentAssertions;
using TIKR.Shared.Enums;
using TIKR.Shared.Helpers;

namespace TIKR.Shared.Tests.Helpers;

public class EmailStructuredExtractorTests
{
    private static string ReadFixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Helpers", "Fixtures", "email", name));

    [Fact]
    public void Parse_ElectionProceduresEml_ExtractsElectionContactAndSuggestion()
    {
        var raw = ReadFixture("election-procedures-update.eml");
        var result = EmailStructuredExtractor.Parse(raw, "election-procedures-update.eml");

        result.Succeeded.Should().BeTrue();
        result.IsElectionRelated.Should().BeTrue();
        result.Subject.Should().Contain("election procedures");
        result.From.Should().Contain("elections@county.example.gov");
        result.Contacts.Should().NotBeEmpty();
        result.Contacts.Should().Contain(c =>
            c.Categories.HasFlag(ContactCategory.Election) &&
            ((c.Email != null && c.Email.Contains("county.example.gov")) ||
             c.Name.Contains("Jordan", StringComparison.OrdinalIgnoreCase)));
        result.RequirementSuggestion.Should().NotBeNull();
        result.RequirementSuggestion!.DueDate.Should().Be(new DateOnly(2026, 11, 5));
        result.ParsedDueOut!.SubmitTo.Should().Contain("County Clerk");
    }

    [Fact]
    public void Parse_ContactChangeEml_ExtractsPocFields()
    {
        var raw = ReadFixture("contact-change.eml");
        var result = EmailStructuredExtractor.Parse(raw, "contact-change.eml");

        result.Succeeded.Should().BeTrue();
        result.IsElectionRelated.Should().BeFalse();
        result.Contacts.Should().Contain(c =>
            c.Name.Contains("Sam", StringComparison.OrdinalIgnoreCase) ||
            c.Email == "sam.rivera@acme-utility.example.com");
        result.ParsedDueOut!.ContactPhone.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Parse_GenericMemo_DoesNotForceElection()
    {
        var raw = ReadFixture("generic-memo.eml");
        var result = EmailStructuredExtractor.Parse(raw, "generic-memo.eml");

        result.Succeeded.Should().BeTrue();
        result.IsElectionRelated.Should().BeFalse();
        result.Contacts.Should().NotBeEmpty(); // still has From mailbox
    }

    [Fact]
    public void Parse_Empty_FailsGracefully()
    {
        var result = EmailStructuredExtractor.Parse("   ");
        result.Succeeded.Should().BeFalse();
        result.Error.Should().NotBeNullOrWhiteSpace();
    }
}
