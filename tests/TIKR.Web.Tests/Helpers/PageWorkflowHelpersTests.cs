using FluentAssertions;
using Microsoft.Extensions.AI;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
using TIKR.Shared.Helpers;
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
    public void FormatDeadlineContext_IncludesDueOutContacts()
    {
        var text = AssistantPromptBuilder.FormatDeadlineContext([
            new DashboardPriority(
                "Liquor license",
                "Renew annually",
                new DateOnly(2026, 3, 15),
                "High",
                SubmitTo: "DOR",
                ContactName: "Jane",
                ContactEmail: "jane@example.com",
                ContactPhone: "303-555-0100")
        ]);

        text.Should().Contain("Submit to: DOR");
        text.Should().Contain("Contact: Jane");
        text.Should().Contain("jane@example.com");
        text.Should().Contain("303-555-0100");
    }

    [Fact]
    public void FormatDueOutContactLine_OmitsEmptyParts()
    {
        AssistantPromptBuilder.FormatDueOutContactLine(null, null, null, null)
            .Should().BeEmpty();
        AssistantPromptBuilder.FormatDueOutContactLine("SOS", null, null, null)
            .Should().Be("Submit to: SOS");
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
    public void FormatPreparingHtml_IsClerkFriendlyStatus()
    {
        var html = AssistantPromptBuilder.FormatPreparingHtml("Looking through your library…");
        html.Should().Contain("tikr-assist-preparing");
        html.Should().Contain("Looking through your library");
        html.Should().Contain("role=\"status\"");
    }

    [Fact]
    public void SanitizeModelOutput_StripsThinkAndToolBlocks()
    {
        var raw =
            "<think>I will call a tool and plan the answer</think>\n" +
            "<tool_call>{\"name\":\"search\"}</tool_call>\n" +
            "The late fee is $25.\n\n**Sources**\n- fee.pdf";

        var clean = AssistantPromptBuilder.SanitizeModelOutput(raw);
        clean.Should().Contain("The late fee is $25.");
        clean.Should().Contain("fee.pdf");
        clean.Should().NotContain("think");
        clean.Should().NotContain("tool_call");
        clean.Should().NotContain("I will call a tool");
    }

    [Fact]
    public void ExtractVisibleStreamingText_HidesIncompleteThinkBlock()
    {
        var partial = "<think>still planning the answer and listing tools";
        AssistantPromptBuilder.ExtractVisibleStreamingText(partial)
            .Should().BeEmpty();

        var closed = partial + "</think>\nPay the form by Friday.";
        AssistantPromptBuilder.ExtractVisibleStreamingText(closed)
            .Should().Be("Pay the form by Friday.");
    }

    [Fact]
    public void SanitizeModelOutput_StripsReActScratchpad_KeepsFinalAnswer()
    {
        var raw =
            "Thought: I need the fee schedule\n" +
            "Action: search_town_documents\n" +
            "Action Input: fee\n" +
            "Observation: found fee.pdf\n" +
            "Final Answer: The filing fee is $40.";

        AssistantPromptBuilder.SanitizeModelOutput(raw)
            .Should().Be("The filing fee is $40.");
    }

    [Fact]
    public void SanitizeModelOutput_KeepsNormalMarkdownCodeFence()
    {
        var raw = "Use this checklist:\n```\n1. Print form\n2. Sign\n```\nThen file.";
        AssistantPromptBuilder.SanitizeModelOutput(raw)
            .Should().Contain("```")
            .And.Contain("Print form")
            .And.Contain("Then file.");
    }

    [Fact]
    public void BuildSystemPrompt_ForbidsScratchpadOutput()
    {
        var catalog = new ColoradoResourceCatalog([], null);
        AssistantPromptBuilder.BuildSystemPrompt(catalog)
            .Should().Contain("Output ONLY the final clerk-facing answer")
            .And.Contain("<think>");
    }

    [Fact]
    public void BuildSystemPrompt_RequiresGroundedAnswers()
    {
        var catalog = new ColoradoResourceCatalog([], null);
        AssistantPromptBuilder.BuildSystemPrompt(catalog)
            .Should().Contain("ONLY from that context")
            .And.Contain("Sources")
            .And.Contain("energetic")
            .And.Contain("product help");
    }

    [Fact]
    public void BuildUserMessageWithRag_IncludesProductHelp()
    {
        var msg = AssistantPromptBuilder.BuildUserMessageWithRag(
            "How do I save?",
            deadlineContext: null,
            docContext: null,
            vaultContext: null,
            searchUnavailable: false,
            citations: [],
            productHelpContext: ProductHelpCatalog.FormatForPrompt(ProductHelpCatalog.Search("save NAS", 1)));

        msg.Should().Contain("TIKR product help");
        msg.Should().Contain("Question: How do I save?");
    }

    [Fact]
    public void BuildProactiveBrief_SummarizesPriorities()
    {
        var brief = AssistantPromptBuilder.BuildProactiveBrief(
            [
                new DashboardPriority("Sales tax", "Due soon", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)), "High"),
                new DashboardPriority("Old item", "Late", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)), "Overdue"),
            ],
            "Deb Dillon");

        brief.Should().NotBeNullOrWhiteSpace();
        brief.Should().Contain("Deb Dillon");
        brief.Should().Contain("overdue");
        brief.Should().Contain("Sales tax");
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
    public void FormatDocumentRagBlock_UsesTopicLabelAboutAndExcerpt()
    {
        var block = AssistantPromptBuilder.FormatDocumentRagBlock(
            new SemanticSearchResponse(
                "retirement",
                1,
                [
                    new SemanticSearchHit(
                        Guid.NewGuid(),
                        "Scanned Document.pdf",
                        "Correspondence",
                        "survivor benefit election section…",
                        0.82,
                        ChunkIndex: 0,
                        Topic: "Retirement Package Form DD-2656",
                        Summary: "Form used to elect survivor benefits under federal retirement systems.")
                ]),
            out var unavailable);

        unavailable.Should().BeFalse();
        block.Should().NotBeNull();
        block.Should().Contain("[Retirement Package Form DD-2656] Scanned Document.pdf");
        block.Should().Contain("About: Form used to elect survivor benefits");
        block.Should().Contain("Excerpt: survivor benefit election section");
        block.Should().NotContain("Scanned Document.pdf [Correspondence]");
    }

    [Fact]
    public void CollectCitationLabels_PrefersTopicPrefixedDocumentNames()
    {
        var labels = AssistantPromptBuilder.CollectCitationLabels(
            new SemanticSearchResponse(
                "q",
                1,
                [
                    new SemanticSearchHit(
                        Guid.NewGuid(),
                        "Scanned Document.pdf",
                        "Correspondence",
                        "snippet",
                        0.9,
                        Topic: "Retirement Package Form DD-2656")
                ]),
            vault: null);

        labels.Should().ContainSingle()
            .Which.Should().Be("[Retirement Package Form DD-2656] Scanned Document.pdf");
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
    public void BuildUserMessageWithRag_WhenNoContextAndSearchAvailable_ReturnsNoHitGuidance()
    {
        var msg = AssistantPromptBuilder.BuildUserMessageWithRag(
            "What is the permit fee?",
            deadlineContext: null,
            docContext: null,
            vaultContext: null,
            searchUnavailable: false,
            citations: []);

        msg.Should().StartWith("What is the permit fee?");
        msg.Should().Contain("No matching documents, vault entries, or product help were retrieved");
        msg.Should().Contain("say so");
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

    [Fact]
    public void LooksLikeFollowUp_DetectsShortAndDeicticPrompts()
    {
        AssistantPromptBuilder.LooksLikeFollowUp("what about the fee?").Should().BeTrue();
        AssistantPromptBuilder.LooksLikeFollowUp("that one").Should().BeTrue();
        AssistantPromptBuilder.LooksLikeFollowUp(
                "What is the mill levy submission process for the annual budget packet?")
            .Should().BeFalse();
    }

    [Fact]
    public void BuildRetrievalQuery_PrependsRecentTurnsForFollowUps()
    {
        var query = AssistantPromptBuilder.BuildRetrievalQuery(
            "what about the fee?",
            ["Where do I submit the liquor license renewal?"]);

        query.Should().Contain("liquor license renewal");
        query.Should().Contain("what about the fee?");
    }

    [Fact]
    public void BuildRetrievalQuery_UsesCurrentAloneWhenNotFollowUp()
    {
        var question = "What is the mill levy submission process for the annual budget packet?";
        AssistantPromptBuilder.BuildRetrievalQuery(question, ["prior unrelated"])
            .Should().Be(question);
    }

    [Fact]
    public void BuildChatMessages_IncludesPriorTurnsAndCurrentRagOnlyOnce()
    {
        var history = new List<ChatMessage>
        {
            new(ChatRole.User, "Where is the liquor license form?"),
            new(ChatRole.Assistant, "See license.pdf in Sources.")
        };
        var ragUser = AssistantPromptBuilder.BuildUserMessageWithRag(
            "what about the fee?",
            deadlineContext: null,
            docContext: "Relevant documents:\n- Source: fee.pdf\n  $125",
            vaultContext: null,
            searchUnavailable: false,
            citations: ["fee.pdf"]);

        var messages = AssistantPromptBuilder.BuildChatMessages("SYS", history, ragUser);

        messages.Should().HaveCount(4);
        messages[0].Role.Should().Be(ChatRole.System);
        messages[1].Text.Should().Be("Where is the liquor license form?");
        messages[1].Text.Should().NotContain("Relevant documents:");
        messages[2].Role.Should().Be(ChatRole.Assistant);
        messages[3].Role.Should().Be(ChatRole.User);
        messages[3].Text.Should().Contain("Relevant documents:");
        messages.Count(m => AssistantPromptBuilder.LooksLikeRagPackedUserMessage(m.Text)).Should().Be(1);
    }

    [Fact]
    public void AppendTurn_CapsHistoryToMaxTurnsWithoutKeepingRagPacks()
    {
        var history = new List<ChatMessage>();
        for (var i = 0; i < 10; i++)
            AssistantPromptBuilder.AppendTurn(history, $"Q{i}", $"A{i}", maxTurns: 3);

        history.Should().HaveCount(6);
        history[0].Text.Should().Be("Q7");
        history.Should().NotContain(m => AssistantPromptBuilder.LooksLikeRagPackedUserMessage(m.Text));
    }

    [Fact]
    public void GetRecentUserTexts_ReturnsLastUserQuestions()
    {
        var history = new List<ChatMessage>
        {
            new(ChatRole.User, "first"),
            new(ChatRole.Assistant, "a1"),
            new(ChatRole.User, "second"),
            new(ChatRole.Assistant, "a2"),
            new(ChatRole.User, "third"),
            new(ChatRole.Assistant, "a3")
        };

        AssistantPromptBuilder.GetRecentUserTexts(history, take: 2)
            .Should().Equal("second", "third");
    }
}
