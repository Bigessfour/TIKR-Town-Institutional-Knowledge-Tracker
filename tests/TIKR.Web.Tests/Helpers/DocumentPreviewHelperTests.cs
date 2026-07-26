using FluentAssertions;
using TIKR.Shared.TestFixtures;
using TIKR.Web.Helpers;

namespace TIKR.Web.Tests.Helpers;

[Trait("Category", TestCategories.FullyTested)]
public class DocumentPreviewHelperTests
{
    [Theory]
    [InlineData("minutes.pdf", "application/pdf", false, DocumentPreviewHelper.PreviewKind.Pdf)]
    [InlineData("memo.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", false, DocumentPreviewHelper.PreviewKind.Word)]
    [InlineData("old.doc", null, false, DocumentPreviewHelper.PreviewKind.Word)]
    [InlineData("budget.xlsx", null, false, DocumentPreviewHelper.PreviewKind.Spreadsheet)]
    [InlineData("sheet.xls", null, false, DocumentPreviewHelper.PreviewKind.Spreadsheet)]
    [InlineData("notes.txt", "text/plain", true, DocumentPreviewHelper.PreviewKind.Text)]
    [InlineData("scan.png", "image/png", false, DocumentPreviewHelper.PreviewKind.ConvertHint)]
    [InlineData("unknown.bin", "application/octet-stream", false, DocumentPreviewHelper.PreviewKind.None)]
    public void ResolveKind_RoutesByExtensionAndContent(
        string fileName,
        string? contentType,
        bool hasText,
        DocumentPreviewHelper.PreviewKind expected)
    {
        DocumentPreviewHelper.ResolveKind(fileName, contentType, hasText).Should().Be(expected);
    }

    [Fact]
    public void LooksLikePdf_RequiresMagicHeader()
    {
        DocumentPreviewHelper.LooksLikePdf("%PDF-1.7"u8).Should().BeTrue();
        DocumentPreviewHelper.LooksLikePdf("not a pdf"u8).Should().BeFalse();
        DocumentPreviewHelper.LooksLikePdf([]).Should().BeFalse();
        DocumentPreviewHelper.LooksLikePdf("%PD"u8).Should().BeFalse();
    }

    [Fact]
    public void PreviewLabel_NamesSdkSurfaces()
    {
        DocumentPreviewHelper.PreviewLabel(DocumentPreviewHelper.PreviewKind.Pdf).Should().Be("PDF Viewer");
        DocumentPreviewHelper.PreviewLabel(DocumentPreviewHelper.PreviewKind.Word).Should().Be("Word preview");
        DocumentPreviewHelper.PreviewLabel(DocumentPreviewHelper.PreviewKind.Spreadsheet).Should().Be("Spreadsheet preview");
    }

    [Fact]
    public void IsLegacyDoc_DetectsDocExtension()
    {
        DocumentPreviewHelper.IsLegacyDoc("a.doc").Should().BeTrue();
        DocumentPreviewHelper.IsLegacyDoc("a.docx").Should().BeFalse();
    }
}
