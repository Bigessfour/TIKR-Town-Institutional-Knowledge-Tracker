using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using TIKR.SyncfusionDocuments;

namespace TIKR.SyncfusionDocuments.Tests;

public class SyncfusionDocumentOcrServiceTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("ab", true)]
    [InlineData("This scanned ordinance has enough letters to skip OCR for a single page.", false)]
    public void NeedsOcr_UsesLetterThreshold(string? text, bool needsOcr)
    {
        SyncfusionDocumentOcrService.NeedsOcr(text).Should().Be(needsOcr);
    }

    [Fact]
    public void EnrichPdf_SkipsOcrWhenNativeTextIsRich_EvenIfEnabled()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["TIKR_OCR_ENABLED"] = "true" })
            .Build();
        var sut = new SyncfusionDocumentOcrService(config, NullLogger<SyncfusionDocumentOcrService>.Instance);
        var rich = new string('a', SyncfusionDocumentOcrService.MinLetterCharsWithoutOcr + 10);
        using var pdf = CreateTextPdf("Ordinance section 12 water rates due annually.");

        var result = sut.EnrichPdf(pdf, rich);

        result.UsedOcr.Should().BeFalse();
        result.Text.Should().Be(rich);
    }

    [Fact]
    public void EnrichPdf_DisabledReturnsExistingText()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["TIKR_OCR_ENABLED"] = "false" })
            .Build();
        var sut = new SyncfusionDocumentOcrService(config, NullLogger<SyncfusionDocumentOcrService>.Instance);
        using var pdf = CreateTextPdf("ignored");

        var result = sut.EnrichPdf(pdf, "sparse");

        result.UsedOcr.Should().BeFalse();
        result.Text.Should().Be("sparse");
        sut.IsEnabled.Should().BeFalse();
    }

    private static MemoryStream CreateTextPdf(string text)
    {
        using var document = new PdfDocument();
        var page = document.Pages.Add();
        page.Graphics.DrawString(text, new PdfStandardFont(PdfFontFamily.Helvetica, 12), PdfBrushes.Black, 10, 10);
        var stream = new MemoryStream();
        document.Save(stream);
        stream.Position = 0;
        return stream;
    }
}
