using FluentAssertions;
using TIKR.Infrastructure.Services;

namespace TIKR.Infrastructure.Tests.Services;

public class StubDocumentAgentExtractionBackendTests
{
    [Fact]
    public async Task ExtractAsync_ReturnsPlainTextForTxtUpload()
    {
        var sut = new StubDocumentAgentExtractionBackend();
        await using var content = new MemoryStream("Wiley budget filing"u8.ToArray());

        var result = await sut.ExtractAsync(content, "notes.txt");

        result.ExtractedText.Should().Contain("Wiley budget filing");
        result.UsedSyncfusionTools.Should().BeFalse();
    }

    [Fact]
    public async Task ExtractAsync_ReturnsStubMessageForBinaryPdf()
    {
        var sut = new StubDocumentAgentExtractionBackend();
        await using var content = new MemoryStream("%PDF-1.4 stub"u8.ToArray());

        var result = await sut.ExtractAsync(content, "report.pdf");

        result.ExtractedText.Should().Contain("Agent stub");
        result.ExtractedText.Should().Contain("report.pdf");
        result.UsedSyncfusionTools.Should().BeFalse();
    }
}
