using FluentAssertions;
using TIKR.Infrastructure.Services;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Tests.Services;

public class SyncfusionDocumentAgentExtractorTests
{
    [Fact]
    public async Task ExtractAsync_UsesPlainTextPathForTxtWithoutSyncfusionTools()
    {
        var storage = new NasSyncfusionDocumentStorage(new InMemoryFileStorage());
        var sut = new SyncfusionDocumentAgentExtractor(storage);
        await using var content = new MemoryStream("Periodic report due March"u8.ToArray());

        var result = await sut.ExtractAsync(content, "report.txt");

        result.ExtractedText.Should().Contain("Periodic report");
        result.UsedSyncfusionTools.Should().BeFalse();
    }

    [Fact]
    public async Task ExtractAsync_ReturnsUnsupportedMessageForUnknownExtension()
    {
        var storage = new NasSyncfusionDocumentStorage(new InMemoryFileStorage());
        var sut = new SyncfusionDocumentAgentExtractor(storage);
        await using var content = new MemoryStream([0x00, 0x01, 0x02]);

        var result = await sut.ExtractAsync(content, "data.xlsx");

        result.ExtractedText.Should().Contain("unsupported type");
        result.UsedSyncfusionTools.Should().BeTrue();
    }

    [Fact]
    public async Task ExtractAsync_ExtractsMinimalPdfWhenSyncfusionAvailable()
    {
        var storage = new NasSyncfusionDocumentStorage(new InMemoryFileStorage());
        var sut = new SyncfusionDocumentAgentExtractor(storage);
        await using var content = new MemoryStream(MinimalPdfBytes);

        var result = await sut.ExtractAsync(content, "minimal.pdf");

        result.UsedSyncfusionTools.Should().BeTrue();
        result.ExtractedText.Should().NotBeNull();
    }

    private static readonly byte[] MinimalPdfBytes =
    """
    %PDF-1.4
    1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj
    2 0 obj<</Type/Pages/Kids[3 0 R]/Count 1>>endobj
    3 0 obj<</Type/Page/MediaBox[0 0 200 200]/Parent 2 0 R/Contents 4 0 R/Resources<</Font<</F1 5 0 R>>>>>>endobj
    4 0 obj<</Length 44>>stream
    BT /F1 12 Tf 50 150 Td (Wiley clerk report) Tj ET
    endstream
    endobj
    5 0 obj<</Type/Font/Subtype/Type1/BaseFont/Helvetica>>endobj
    xref
    0 6
    0000000000 65535 f
    0000000009 00000 n
    0000000058 00000 n
    0000000115 00000 n
    0000000261 00000 n
    0000000354 00000 n
    trailer<</Size 6/Root 1 0 R>>
    startxref
    422
    %%EOF
    """u8.ToArray();

    private sealed class InMemoryFileStorage : IFileStorageService
    {
        private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

        public Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
        {
            using var ms = new MemoryStream();
            content.CopyTo(ms);
            _files[fileName] = ms.ToArray();
            return Task.FromResult(fileName);
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream(_files[storagePath]));

        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            _files.Remove(storagePath);
            return Task.CompletedTask;
        }

        public string GetFullPath(string storagePath) => storagePath;
    }
}
