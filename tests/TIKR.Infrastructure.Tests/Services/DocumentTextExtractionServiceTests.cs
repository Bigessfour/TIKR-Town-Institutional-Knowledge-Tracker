using FluentAssertions;
using TIKR.Infrastructure.Services;

namespace TIKR.Infrastructure.Tests.Services;

public class DocumentTextExtractionServiceTests
{
    [Theory]
    [InlineData("notes.txt", true)]
    [InlineData("report.PDF", false)]
    [InlineData("data.csv", true)]
    public void CanExtract_RecognizesTextExtensions(string fileName, bool expected) =>
        DocumentTextExtractionService.CanExtract(fileName).Should().Be(expected);

    [Fact]
    public async Task TryExtractAsync_ReturnsUtf8TextForTxt()
    {
        await using var stream = new MemoryStream("Ordinance filing deadline March 15"u8.ToArray());
        var text = await DocumentTextExtractionService.TryExtractAsync(stream, "ordinance.txt");
        text.Should().Be("Ordinance filing deadline March 15");
    }

    [Fact]
    public async Task TryExtractAsync_ReturnsNullForPdf()
    {
        await using var stream = new MemoryStream("%PDF-1.4"u8.ToArray());
        var text = await DocumentTextExtractionService.TryExtractAsync(stream, "scan.pdf");
        text.Should().BeNull();
    }
}
