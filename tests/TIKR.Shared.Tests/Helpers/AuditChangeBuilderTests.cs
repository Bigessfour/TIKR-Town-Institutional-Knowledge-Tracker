using FluentAssertions;
using TIKR.Shared.Helpers;

namespace TIKR.Shared.Tests.Helpers;

public class AuditChangeBuilderTests
{
    [Fact]
    public void Build_WithNoChanges_ReturnsSummaryOnly()
    {
        AuditChangeBuilder.Build("Budget ordinance", ("Title", "A", "A"))
            .Should().Be("Budget ordinance");
    }

    [Fact]
    public void Build_WithFieldDiffs_ReturnsJsonPayload()
    {
        var json = AuditChangeBuilder.Build(
            "Budget ordinance",
            ("Title", "Old", "New"),
            ("IsCompleted", false, true));

        json.Should().Contain("\"summary\":\"Budget ordinance\"");
        json.Should().Contain("\"Title\"");
        json.Should().Contain("\"from\":\"Old\"");
        json.Should().Contain("\"to\":\"New\"");
        json.Should().Contain("\"IsCompleted\"");
    }
}

public class AuditDetailsFormatterTests
{
    [Fact]
    public void Format_PlainText_Passthrough()
    {
        AuditDetailsFormatter.Format("Created requirement")
            .Should().Be("Created requirement");
    }

    [Fact]
    public void Format_JsonDiffs_ReadableSummary()
    {
        var details = AuditChangeBuilder.Build("Budget", ("Title", "A", "B"));
        var formatted = AuditDetailsFormatter.Format(details);
        formatted.Should().Contain("Budget");
        formatted.Should().Contain("Title:");
        formatted.Should().Contain("A → B");
    }

    [Fact]
    public void Format_FeatureSettingsDump_HidesPaths()
    {
        var raw = "UseGrok=True; OllamaHost=http://127.0.0.1:11434; Storage=/Users/me/.local-data/documents; SyncfusionConfigured=True";
        var formatted = AuditDetailsFormatter.Format(raw);
        formatted.Should().Be("Town helper settings saved (Advanced AI on)");
        formatted.Should().NotContain("OllamaHost");
        formatted.Should().NotContain("/Users/");
    }

    [Fact]
    public void FormatCorpusAttention_SummarizesFilenames()
    {
        var text = AuditDetailsFormatter.FormatCorpusAttention(
        [
            "2 document(s) ready for reindex (Ollama offline earlier?)",
            "a.pdf",
            "b.pdf",
            "c.pdf",
        ]);
        text.Should().Contain("reindex");
        text.Should().Contain("3 scanned files");
        text.Should().Contain("+1 more");
    }
}
