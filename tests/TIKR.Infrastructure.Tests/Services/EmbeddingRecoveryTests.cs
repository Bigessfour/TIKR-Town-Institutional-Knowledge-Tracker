using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TIKR.Infrastructure.Services;
using TIKR.Infrastructure.Tests.Helpers;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Interfaces;
using TIKR.Shared.TestFixtures;

namespace TIKR.Infrastructure.Tests.Services;

[Trait("Category", TestCategories.FullyTested)]
public class EmbeddingRecoveryTests
{
    private static readonly GrokService DisabledGrok = TestGrokServiceFactory.CreateDisabled();

    [Fact]
    public void NeedsRecovery_WhenEmbeddableDocMissingChunks_IsTrue()
    {
        var health = new CorpusHealthResponse(
            DocumentsTotal: 2,
            DocumentsWithChunks: 1,
            DocumentsTransient: 0,
            DocumentsSparseText: 0,
            KnowledgeTotal: 0,
            KnowledgeWithChunks: 0,
            DocumentsChunkCoveragePercent: 50,
            KnowledgeChunkCoveragePercent: 100,
            NeedsAttention: []);
        EmbeddingRecoveryState.NeedsRecovery(health).Should().BeTrue();
    }

    [Fact]
    public void NeedsRecovery_WhenOnlySparseGaps_IsFalse()
    {
        var health = new CorpusHealthResponse(
            DocumentsTotal: 2,
            DocumentsWithChunks: 1,
            DocumentsTransient: 0,
            DocumentsSparseText: 1,
            KnowledgeTotal: 1,
            KnowledgeWithChunks: 1,
            DocumentsChunkCoveragePercent: 50,
            KnowledgeChunkCoveragePercent: 100,
            NeedsAttention: ["scan.pdf"]);
        EmbeddingRecoveryState.NeedsRecovery(health).Should().BeFalse();
    }

    [Fact]
    public void NeedsRecovery_WhenComplete_IsFalse()
    {
        var health = new CorpusHealthResponse(
            DocumentsTotal: 3,
            DocumentsWithChunks: 3,
            DocumentsTransient: 0,
            DocumentsSparseText: 0,
            KnowledgeTotal: 2,
            KnowledgeWithChunks: 2,
            DocumentsChunkCoveragePercent: 100,
            KnowledgeChunkCoveragePercent: 100,
            NeedsAttention: []);
        EmbeddingRecoveryState.NeedsRecovery(health).Should().BeFalse();
    }

    [Fact]
    public async Task ReindexAllEmbeddingsAsync_SkipsDeletedAndTransient()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var activeId = Guid.NewGuid();
        db.Documents.AddRange(
            new Document
            {
                Id = activeId,
                FileName = "active.pdf",
                StoragePath = "p/active.pdf",
                FullTextContent = "active recurring document with enough letters for embedding gate threshold",
                UploadedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Document
            {
                Id = Guid.NewGuid(),
                FileName = "gone.pdf",
                StoragePath = "p/gone.pdf",
                FullTextContent = "deleted document with enough letters for embedding gate threshold here",
                DeletedAt = DateTime.UtcNow,
                UploadedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Document
            {
                Id = Guid.NewGuid(),
                FileName = "temp.pdf",
                StoragePath = "p/temp.pdf",
                FullTextContent = "transient document with enough letters for embedding gate threshold here",
                IsTransient = true,
                UploadedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var sut = CreateSut(db, text => Enumerable.Range(0, 16).Select(i => (float)(text.Length + i)).ToArray());
        var result = await sut.ReindexAllEmbeddingsAsync(trigger: "test");
        result.DocumentsAttempted.Should().Be(1);
        result.DocumentsEmbedded.Should().Be(1);
        result.DocumentsSkipped.Should().Be(2);
        result.Trigger.Should().Be("test");
        db.EmbeddingChunks.Count(c => c.SourceId == activeId).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetCorpusHealthAsync_ExcludesSoftDeletedFromTotals()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        db.Documents.AddRange(
            new Document
            {
                Id = Guid.NewGuid(),
                FileName = "live.pdf",
                StoragePath = "p/live.pdf",
                FullTextContent = "live document body with enough letter characters for health",
                UploadedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Document
            {
                Id = Guid.NewGuid(),
                FileName = "bin.pdf",
                StoragePath = "p/bin.pdf",
                FullTextContent = "in recycle bin with enough letter characters for health checks",
                DeletedAt = DateTime.UtcNow,
                UploadedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var sut = CreateSut(db, _ => Enumerable.Range(0, 16).Select(i => (float)i).ToArray());
        var health = await sut.GetCorpusHealthAsync();
        health.DocumentsTotal.Should().Be(1);
    }

    [Fact]
    public void EmbeddingRecoveryState_NoteReindex_SnapshotsSummary()
    {
        var state = new EmbeddingRecoveryState();
        state.NoteOllama(true, DateTime.UtcNow);
        state.NoteReindexResult(
            "auto-recovery-ollama",
            new ReindexEmbeddingsResponse(2, 2, 1, 1, [], DocumentsSkipped: 1, Trigger: "auto-recovery-ollama"),
            DateTime.UtcNow);

        var snap = state.Snapshot();
        snap.OllamaAvailable.Should().BeTrue();
        snap.LastTrigger.Should().Be("auto-recovery-ollama");
        snap.LastResultSummary.Should().Contain("docs 2/2");
        snap.LastResultSummary.Should().Contain("skipped 1");
    }

    private static HybridAiService CreateSut(
        TIKR.Infrastructure.Data.TikrDbContext db,
        Func<string, float[]?> embedder)
    {
        var factory = new Mock<IOllamaChatClientFactory>();
        factory.Setup(f => f.CreateChatClient()).Returns(new StubChatClient(""));
        factory.SetupGet(f => f.ChatModel).Returns("test-model");
        factory.Setup(f => f.CreateEmbeddingGenerator(It.IsAny<string>()))
            .Returns(new StubEmbeddingGenerator(embedder));
        return new HybridAiService(
            db,
            factory.Object,
            DisabledGrok,
            Mock.Of<IFileStorageService>(),
            Mock.Of<IDocumentAgentExtractionBackend>(),
            NullLogger<HybridAiService>.Instance);
    }
}
