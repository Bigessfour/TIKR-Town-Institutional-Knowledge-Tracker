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
    public void FormatStreamingHtml_EncodesMarkup()
    {
        var html = AssistantPromptBuilder.FormatStreamingHtml("Hello <world> & more");
        html.Should().Contain("tikr-assist-stream");
        html.Should().Contain("Hello &lt;world&gt; &amp; more");
        html.Should().NotContain("<world>");
    }

    [Fact]
    public void BuildSystemPrompt_RequiresGroundedAnswers()
    {
        var catalog = new ColoradoResourceCatalog([], null);
        AssistantPromptBuilder.BuildSystemPrompt(catalog)
            .Should().Contain("ONLY from that context")
            .And.Contain("Sources");
    }

    [Fact]
    public void FormatDocumentRagBlock_MarksUnavailable()
    {
        var block = AssistantPromptBuilder.FormatDocumentRagBlock(
            new SemanticSearchResponse("q", 0, [], EmbeddingAvailable: false),
            out var unavailable);
        unavailable.Should().BeTrue();
        block.Should().BeNull();
    }

    [Fact]
    public void BuildUserMessageWithRag_IncludesCitationsAndNoHitGuidance()
    {
        var msg = AssistantPromptBuilder.BuildUserMessageWithRag(
            "What is the fee?",
            deadlineContext: null,
            docContext: "Relevant documents:\n- Source: fee.pdf\n  $125",
            vaultContext: null,
            searchUnavailable: false,
            citations: ["fee.pdf"]);

        msg.Should().Contain("Question: What is the fee?");
        msg.Should().Contain("fee.pdf");
        msg.Should().Contain("Required Sources");
    }

    [Fact]
    public void BuildUserMessageWithRag_WhenSearchDown_WarnsClerk()
    {
        var msg = AssistantPromptBuilder.BuildUserMessageWithRag(
            "help",
            null,
            null,
            null,
            searchUnavailable: true,
            citations: []);

        msg.Should().Contain("temporarily unavailable");
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
        DocumentUiMessages.DownloadInProgress("a.pdf").Should().Contain("Downloading");
        DocumentUiMessages.SemanticSearchFailed("timeout").Should().Contain("timeout");
        DocumentUiMessages.GenerationFailed(null).Should().Contain("Syncfusion");
        DocumentUiMessages.CanConvertToPdf("memo.docx").Should().BeTrue();
        DocumentUiMessages.CanConvertToPdf("budget.pdf").Should().BeFalse();
    }
}
