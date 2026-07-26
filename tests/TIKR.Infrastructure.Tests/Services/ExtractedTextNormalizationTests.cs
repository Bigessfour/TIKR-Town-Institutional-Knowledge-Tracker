using FluentAssertions;
using TIKR.Infrastructure.Services;

namespace TIKR.Infrastructure.Tests.Services;

public class ExtractedTextNormalizationTests
{
    [Fact]
    public void NormalizeExtractedText_StripsDocumentIdObjectDump()
    {
        var dump =
            "{ DocumentId = a09d1fc65147413c9ccf64b99712d63c_irs-fw9.pdf, Text = --- Page 1 ---\nForm  W-9\r\n(Rev. March 2024)\nRequest for Taxpayer Identification Number, PageCount = 1 }";

        var text = SyncfusionDocumentAgentExtractor.NormalizeExtractedText(dump);

        text.Should().NotBeNullOrWhiteSpace();
        text.Should().StartWith("--- Page 1 ---");
        text.Should().Contain("Form  W-9");
        text.Should().NotContain("DocumentId =");
        text.Should().NotContain("PageCount");
    }

    [Fact]
    public void NormalizeExtractedText_StripsTextOnlyObjectDump()
    {
        var dump = "{ Text = Created with a trial version of Syncfusion Word library }";

        var text = SyncfusionDocumentAgentExtractor.NormalizeExtractedText(dump);

        text.Should().Be("Created with a trial version of Syncfusion Word library");
    }

    [Fact]
    public void NormalizeExtractedText_LeavesPlainPassageUnchanged()
    {
        const string plain = "--- Page 1 ---\nForm W-9 Request for Taxpayer Identification Number";

        SyncfusionDocumentAgentExtractor.NormalizeExtractedText(plain).Should().Be(plain);
    }

    [Fact]
    public void ExtractTextFromData_ReadsTextProperty()
    {
        var payload = new { DocumentId = "x.pdf", Text = "--- Page 1 ---\nHello clerk", PageCount = 1 };

        var text = SyncfusionDocumentAgentExtractor.ExtractTextFromData(payload);

        text.Should().Be("--- Page 1 ---\nHello clerk");
    }

    [Fact]
    public void ExtractTextFromData_StringPassthrough()
    {
        SyncfusionDocumentAgentExtractor.ExtractTextFromData("plain extract")
            .Should().Be("plain extract");
    }
}
