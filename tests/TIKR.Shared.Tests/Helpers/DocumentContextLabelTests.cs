using FluentAssertions;
using TIKR.Shared.Helpers;

namespace TIKR.Shared.Tests.Helpers;

public class DocumentContextLabelTests
{
    [Fact]
    public void InferTopic_FromFormTitleAndDdNumber()
    {
        var text = """
            DATA FOR PAYMENT OF RETIRED PERSONNEL
            Form DD-2656
            This form is used to elect survivor benefits under CSRS/FERS retirement.
            """;

        var topic = DocumentContextLabel.InferTopic("Scanned Document.pdf", text);

        topic.Should().NotBeNullOrWhiteSpace();
        topic.Should().ContainEquivalentOf("DD-2656");
        topic.Should().ContainEquivalentOf("RETIRED PERSONNEL");
    }

    [Fact]
    public void BuildSourceLabel_PrefixesTopicForGenericScanName()
    {
        var label = DocumentContextLabel.BuildSourceLabel(
            "Scanned Document.pdf",
            "Retirement Package Form DD-2656");

        label.Should().Be("[Retirement Package Form DD-2656] Scanned Document.pdf");
    }

    [Fact]
    public void BuildSourceLabel_SkipsRedundantTopicMatchingFileStem()
    {
        DocumentContextLabel.BuildSourceLabel("water-rate.pdf", "water-rate")
            .Should().Be("water-rate.pdf");
    }

    [Fact]
    public void BuildSummary_ReturnsOrientationExcerpt()
    {
        var summary = DocumentContextLabel.BuildSummary(
            "Open Meetings Law Compliance Review Checklist. Use this checklist before each board meeting. " +
            "Verify notice posting timelines and agenda distribution.");

        summary.Should().NotBeNullOrWhiteSpace();
        summary.Should().Contain("Open Meetings Law");
        summary!.Length.Should().BeLessThanOrEqualTo(DocumentContextLabel.DefaultSummaryMaxLen + 1);
    }

    [Fact]
    public void FormatSourceHeader_UsesTopicFolderAndPassage()
    {
        var header = DocumentContextLabel.FormatSourceHeader(
            "Scanned Document.pdf",
            "Retirement Package Form DD-2656",
            "Correspondence",
            chunkIndex: 0);

        header.Should().Be(
            "[Retirement Package Form DD-2656] Scanned Document.pdf — Correspondence · passage 1");
    }

    [Fact]
    public void FormatRagHit_IncludesAboutAndExcerpt()
    {
        var block = DocumentContextLabel.FormatRagHit(
            "Scanned Document.pdf",
            "Open Meetings Law Compliance Review Checklist",
            "Governance",
            chunkIndex: 0,
            summary: "Checklist for municipal open-meetings notice and agenda posting.",
            snippet: "Post notice at least 24 hours before the meeting…");

        block.Should().Contain("[Open Meetings Law Compliance Review Checklist] Scanned Document.pdf");
        block.Should().Contain("About: Checklist for municipal open-meetings");
        block.Should().Contain("Excerpt: Post notice at least 24 hours");
    }

    [Fact]
    public void InferTopic_FallsBackToDescriptiveFileStem()
    {
        DocumentContextLabel.InferTopic("cml-governance-101.pdf", fullTextContent: null)
            .Should().Be("cml-governance-101");
    }

    [Fact]
    public void InferTopic_IgnoresGenericScanStemWithoutBody()
    {
        DocumentContextLabel.InferTopic("Scanned Document.pdf", fullTextContent: null)
            .Should().BeNull();
    }

    [Fact]
    public void InferTopic_UsesAiTagsWhenBodyMissing()
    {
        DocumentContextLabel.InferTopic(
                "scan.pdf",
                fullTextContent: null,
                aiTags: """["retirement-package","personnel"]""")
            .Should().ContainEquivalentOf("Retirement package");
    }
}
