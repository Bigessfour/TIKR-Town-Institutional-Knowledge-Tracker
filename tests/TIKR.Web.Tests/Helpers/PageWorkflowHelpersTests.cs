using FluentAssertions;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
using TIKR.Web.Helpers;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Helpers;

public class PageWorkflowHelpersTests
{
    [Fact]
    public void BuildSystemPrompt_IncludesCatalogWhenPresent()
    {
        var catalog = new ColoradoResourceCatalog([
            new ColoradoResource("CML", "https://www.cml.org", "organization", [], "League")
        ], "2026-01-01");

        AssistantPromptBuilder.BuildSystemPrompt(catalog)
            .Should().Contain("CML")
            .And.Contain("https://www.cml.org");
    }

    [Fact]
    public void BuildSystemPrompt_OmitsCatalogBlockWhenEmpty()
    {
        var catalog = new ColoradoResourceCatalog([], null);
        AssistantPromptBuilder.BuildSystemPrompt(catalog).Should().NotContain("Trusted external sources");
    }

    [Fact]
    public void FormatDeadlineContext_IncludesDueDates()
    {
        var text = AssistantPromptBuilder.FormatDeadlineContext([
            new DashboardPriority("Budget", "Submit", new DateOnly(2026, 12, 1), "High")
        ]);

        text.Should().Contain("Budget (High): Submit — due Dec 1");
    }

    [Fact]
    public void VaultCopyBuilder_IncludesAllSections()
    {
        var howTo = new[] { new KnowledgeEntryDto(Guid.NewGuid(), "Open safe", "Combo", KnowledgeCategory.HowTo, 0) };
        var text = VaultCopyBuilder.BuildCopyAllText(
            howTo, [], [], [
                ("Voice memo", new DateTime(2026, 6, 28, 9, 0, 0), "Remember the safe combo")
            ]);

        text.Should().Contain("HOW-TO");
        text.Should().Contain("Open safe");
        text.Should().Contain("VOICE NOTES");
        text.Should().Contain("Voice memo");
        text.Should().Contain("FOR THE NEW CLERK");
    }

    [Fact]
    public void FilterCategory_OrdersBySortOrder()
    {
        var entries = new[]
        {
            new KnowledgeEntryDto(Guid.NewGuid(), "b", "2", KnowledgeCategory.Contact, 2),
            new KnowledgeEntryDto(Guid.NewGuid(), "a", "1", KnowledgeCategory.Contact, 1)
        };

        VaultCopyBuilder.FilterCategory(entries, KnowledgeCategory.Contact)
            .Select(e => e.Title)
            .Should().ContainInOrder("a", "b");
    }

    [Fact]
    public void DocumentUiMessages_FormatsUserFacingStrings()
    {
        DocumentUiMessages.UploadSuccess("a.pdf").Should().Contain("a.pdf");
        DocumentUiMessages.UploadFailure("a.pdf").Should().Contain("Failed");
        DocumentUiMessages.BulkDelete(3).Should().Contain("3");
        DocumentUiMessages.BulkRetag(2).Should().Contain("Re-tagged");
        DocumentUiMessages.SuggestionAccepted().Should().Contain("accepted");
        DocumentUiMessages.DownloadSuccess("a.pdf").Should().Contain("a.pdf");
        DocumentUiMessages.DownloadFailed("a.pdf").Should().Contain("a.pdf");
        DocumentUiMessages.SemanticSearchFailed("timeout").Should().Contain("timeout");
    }
}
