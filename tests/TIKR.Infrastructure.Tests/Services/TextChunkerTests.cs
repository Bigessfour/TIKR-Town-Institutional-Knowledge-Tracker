using FluentAssertions;
using TIKR.Infrastructure.Services;
using TIKR.Shared.TestFixtures;

namespace TIKR.Infrastructure.Tests.Services;

[Trait("Category", TestCategories.FullyTested)]
public class TextChunkerTests
{
    [Fact]
    public void Chunk_ShortText_ReturnsSingleChunk()
    {
        var chunks = TextChunker.Chunk("short text");
        chunks.Should().ContainSingle().Which.Should().Be("short text");
    }

    [Fact]
    public void Chunk_LongText_ProducesMultipleOverlappingChunks()
    {
        var text = string.Join("\n\n", Enumerable.Range(0, 40).Select(i => $"Paragraph {i} with enough words to grow."));
        var chunks = TextChunker.Chunk(text, chunkSize: 200, overlap: 40);
        chunks.Count.Should().BeGreaterThan(1);
        chunks.Sum(c => c.Length).Should().BeGreaterThan(text.Length);
    }

    [Fact]
    public void Chunk_Empty_ReturnsEmpty()
    {
        TextChunker.Chunk("   ").Should().BeEmpty();
    }

    [Fact]
    public void Sha256Hex_IsStable()
    {
        TextChunker.Sha256Hex("abc").Should().Be(TextChunker.Sha256Hex("abc"));
        TextChunker.Sha256Hex("abc").Should().NotBe(TextChunker.Sha256Hex("abd"));
    }
}
