using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TIKR.Infrastructure.Services;
using TIKR.Infrastructure.Tests.Helpers;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;
using TIKR.Shared.Interfaces;
using TIKR.Shared.TestFixtures;

namespace TIKR.Infrastructure.Tests.Services;

[Trait("Category", TestCategories.FullyTested)]
public class HybridAiServiceSemanticSearchTests
{
    private static readonly GrokService DisabledGrok = TestGrokServiceFactory.CreateDisabled();

    [Fact]
    public void CosineSimilarity_IdenticalVectors_ReturnsOne()
    {
        var v = new[] { 1f, 2f, 3f };
        HybridAiService.CosineSimilarity(v, v).Should().BeApproximately(1.0, 1e-6);
    }

    [Fact]
    public void CosineSimilarity_OrthogonalVectors_ReturnsZero()
    {
        var a = new[] { 1f, 0f, 0f };
        var b = new[] { 0f, 1f, 0f };
        HybridAiService.CosineSimilarity(a, b).Should().BeApproximately(0.0, 1e-6);
    }

    [Fact]
    public void CosineSimilarity_MismatchedLengths_ReturnsZero()
    {
        HybridAiService.CosineSimilarity([1f, 2f], [1f, 2f, 3f]).Should().Be(0);
    }

    [Fact]
    public void PackUnpackFloats_RoundTrips()
    {
        var original = new[] { 0.1f, -0.2f, 3.14159f, 1000f };
        var bytes = HybridAiService.PackFloats(original);
        var restored = HybridAiService.UnpackFloats(bytes);
        restored.Should().Equal(original);
    }

    [Fact]
    public async Task EmbedDocumentAsync_StoresEmbeddingBytes()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            FileName = "budget.pdf",
            StoragePath = "p/budget.pdf",
            FullTextContent = "annual operating budget for fiscal year",
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Documents.Add(doc);
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactoryWithEmbedder(text => Vector(text));
        var sut = new HybridAiService(db, ollama, DisabledGrok, Mock.Of<IFileStorageService>(), Mock.Of<IDocumentAgentExtractionBackend>(), NullLogger<HybridAiService>.Instance);

        var result = await sut.EmbedDocumentAsync(doc.Id);
        result.Embedded.Should().BeTrue();

        var reloaded = await db.Documents.FindAsync(doc.Id);
        reloaded!.Embedding.Should().NotBeNull();
        reloaded.Embedding!.Length.Should().Be(VectorDimensions * sizeof(float));
    }

    [Fact]
    public async Task EmbedDocumentAsync_ReturnsFailureWhenGeneratorThrows()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            FileName = "scan.pdf",
            StoragePath = "p/scan.pdf",
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Documents.Add(doc);
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactoryWithEmbedder(_ => null); // null vector => stub throws
        var sut = new HybridAiService(db, ollama, DisabledGrok, Mock.Of<IFileStorageService>(), Mock.Of<IDocumentAgentExtractionBackend>(), NullLogger<HybridAiService>.Instance);

        var result = await sut.EmbedDocumentAsync(doc.Id);
        result.Embedded.Should().BeFalse();
        result.Reason.Should().NotBeNullOrWhiteSpace();

        var reloaded = await db.Documents.FindAsync(doc.Id);
        reloaded!.Embedding.Should().BeNull();
    }

    [Fact]
    public async Task SemanticSearchDocumentsAsync_RanksByCosineSimilarity()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var budgetDoc = SeedWithEmbedding(db, "budget.pdf", "annual operating budget");
        var minutesDoc = SeedWithEmbedding(db, "minutes.pdf", "council meeting minutes notes");
        var ordinanceDoc = SeedWithEmbedding(db, "ordinance.pdf", "zoning ordinance amendment");
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactoryWithEmbedder(text => Vector(text));
        var sut = new HybridAiService(db, ollama, DisabledGrok, Mock.Of<IFileStorageService>(), Mock.Of<IDocumentAgentExtractionBackend>(), NullLogger<HybridAiService>.Instance);

        var response = await sut.SemanticSearchDocumentsAsync(new SemanticSearchRequest("budget", 2, MinScore: 0));

        response.EmbeddingAvailable.Should().BeTrue();
        response.Considered.Should().Be(3);
        response.Hits.Should().HaveCount(2);
        // The budget doc shares the "budget" token and must rank first.
        response.Hits[0].DocumentId.Should().Be(budgetDoc.Id);
        response.Hits[0].Score.Should().BeGreaterThan(response.Hits[1].Score);
    }

    [Fact]
    public async Task SemanticSearchDocumentsAsync_SkipsDocumentsWithoutEmbedding()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        SeedWithEmbedding(db, "with-embedding.pdf", "budget");
        db.Documents.Add(new Document
        {
            Id = Guid.NewGuid(),
            FileName = "no-embedding.pdf",
            StoragePath = "p/no.pdf",
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactoryWithEmbedder(text => Vector(text));
        var sut = new HybridAiService(db, ollama, DisabledGrok, Mock.Of<IFileStorageService>(), Mock.Of<IDocumentAgentExtractionBackend>(), NullLogger<HybridAiService>.Instance);

        var response = await sut.SemanticSearchDocumentsAsync(new SemanticSearchRequest("budget", 5, MinScore: 0));
        response.Considered.Should().Be(1);
        response.Hits.Should().ContainSingle()
            .Which.FileName.Should().Be("with-embedding.pdf");
    }

    [Fact]
    public async Task SemanticSearchDocumentsAsync_ReturnsEmptyWhenGeneratorFails()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        SeedWithEmbedding(db, "any.pdf", "content");
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactoryWithEmbedder(_ => null);
        var sut = new HybridAiService(db, ollama, DisabledGrok, Mock.Of<IFileStorageService>(), Mock.Of<IDocumentAgentExtractionBackend>(), NullLogger<HybridAiService>.Instance);

        var response = await sut.SemanticSearchDocumentsAsync(new SemanticSearchRequest("query", 3));
        response.Hits.Should().BeEmpty();
        response.EmbeddingAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task SemanticSearchDocumentsAsync_FiltersBelowMinScore()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        SeedWithEmbedding(db, "budget.pdf", "annual operating budget");
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactoryWithEmbedder(text => Vector(text));
        var sut = new HybridAiService(db, ollama, DisabledGrok, Mock.Of<IFileStorageService>(), Mock.Of<IDocumentAgentExtractionBackend>(), NullLogger<HybridAiService>.Instance);

        var response = await sut.SemanticSearchDocumentsAsync(new SemanticSearchRequest("zzzzunrelated", 5, MinScore: 0.99));
        response.Hits.Should().BeEmpty();
    }

    [Fact]
    public async Task EmbedDocumentAsync_IndexesChunksForLongText()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var marker = "UNIQUE_LATE_SECTION_MARKER_XYZ";
        var body = new string('a', 4500) + " " + marker + " building permit late fee schedule";
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            FileName = "long-ordinance.pdf",
            StoragePath = "p/long.pdf",
            FullTextContent = body,
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Documents.Add(doc);
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactoryWithEmbedder(text => Vector(text));
        var sut = new HybridAiService(db, ollama, DisabledGrok, Mock.Of<IFileStorageService>(), Mock.Of<IDocumentAgentExtractionBackend>(), NullLogger<HybridAiService>.Instance);

        var embedded = await sut.EmbedDocumentAsync(doc.Id);
        embedded.Embedded.Should().BeTrue();

        var chunkCount = db.EmbeddingChunks.Count(c => c.SourceId == doc.Id);
        chunkCount.Should().BeGreaterThan(1);

        var response = await sut.SemanticSearchDocumentsAsync(new SemanticSearchRequest(marker, 3, MinScore: 0));
        response.Hits.Should().Contain(h => h.DocumentId == doc.Id && h.Snippet != null && h.Snippet.Contains(marker));
    }

    [Fact]
    public async Task SemanticSearchDocumentsAsync_ExactFileNameRanksFirst()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        SeedWithEmbedding(db, "fee-schedule-2026.pdf", "municipal fees and charges overview");
        SeedWithEmbedding(db, "other.pdf", "municipal fees and charges overview");
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactoryWithEmbedder(text => Vector(text));
        var sut = new HybridAiService(db, ollama, DisabledGrok, Mock.Of<IFileStorageService>(), Mock.Of<IDocumentAgentExtractionBackend>(), NullLogger<HybridAiService>.Instance);

        var response = await sut.SemanticSearchDocumentsAsync(new SemanticSearchRequest("fee-schedule-2026", 2, MinScore: 0));
        response.Hits[0].FileName.Should().Be("fee-schedule-2026.pdf");
    }

    [Fact]
    public async Task SemanticSearchDocumentsAsync_FolderFilterExcludesOthers()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var permits = SeedWithEmbedding(db, "a.pdf", "permit fee");
        permits.SuggestedFolder = "Permits";
        var other = SeedWithEmbedding(db, "b.pdf", "permit fee");
        other.SuggestedFolder = "Minutes";
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactoryWithEmbedder(text => Vector(text));
        var sut = new HybridAiService(db, ollama, DisabledGrok, Mock.Of<IFileStorageService>(), Mock.Of<IDocumentAgentExtractionBackend>(), NullLogger<HybridAiService>.Instance);

        var response = await sut.SemanticSearchDocumentsAsync(new SemanticSearchRequest("permit", 5, MinScore: 0, Folder: "Permits"));
        response.Hits.Should().ContainSingle().Which.DocumentId.Should().Be(permits.Id);
    }

    [Fact]
    public async Task SemanticSearchDocumentsAsync_MergesChunkedAndLegacySources()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var legacy = SeedWithEmbedding(db, "legacy-budget.pdf", "annual operating budget legacy");
        var chunked = new Document
        {
            Id = Guid.NewGuid(),
            FileName = "chunked-budget.pdf",
            StoragePath = "p/chunked.pdf",
            FullTextContent = "annual operating budget chunked",
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Documents.Add(chunked);
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactoryWithEmbedder(text => Vector(text));
        var sut = new HybridAiService(db, ollama, DisabledGrok, Mock.Of<IFileStorageService>(), Mock.Of<IDocumentAgentExtractionBackend>(), NullLogger<HybridAiService>.Instance);
        (await sut.EmbedDocumentAsync(chunked.Id)).Embedded.Should().BeTrue();

        var response = await sut.SemanticSearchDocumentsAsync(new SemanticSearchRequest("budget", 5, MinScore: 0));
        response.Considered.Should().Be(2);
        response.Hits.Select(h => h.DocumentId).Should().Contain([legacy.Id, chunked.Id]);
    }

    [Fact]
    public async Task DocumentDelete_RemovesEmbeddingChunks()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            FileName = "gone.pdf",
            StoragePath = "p/gone.pdf",
            FullTextContent = "delete me content",
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Documents.Add(doc);
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactoryWithEmbedder(text => Vector(text));
        var ai = new HybridAiService(db, ollama, DisabledGrok, Mock.Of<IFileStorageService>(), Mock.Of<IDocumentAgentExtractionBackend>(), NullLogger<HybridAiService>.Instance);
        (await ai.EmbedDocumentAsync(doc.Id)).Embedded.Should().BeTrue();
        db.EmbeddingChunks.Count(c => c.SourceId == doc.Id).Should().BeGreaterThan(0);

        var docs = new DocumentService(db);
        var audit = new Mock<IAuditService>();
        audit.Setup(a => a.LogAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var user = new Mock<ICurrentUserService>();
        user.SetupGet(u => u.UserId).Returns("tester");

        await docs.DeleteAsync(doc.Id, Mock.Of<IFileStorageService>(), audit.Object, user.Object);
        db.EmbeddingChunks.Count(c => c.SourceId == doc.Id).Should().Be(0);
    }

    [Fact]
    public async Task EmbedDocumentAsync_SkipsWhenContentHashUnchanged()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            FileName = "stable.pdf",
            StoragePath = "p/stable.pdf",
            FullTextContent = "unchanged content for hash skip",
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Documents.Add(doc);
        await db.SaveChangesAsync();

        var calls = 0;
        var ollama = CreateOllamaFactoryWithEmbedder(text =>
        {
            calls++;
            return Vector(text);
        });
        var sut = new HybridAiService(db, ollama, DisabledGrok, Mock.Of<IFileStorageService>(), Mock.Of<IDocumentAgentExtractionBackend>(), NullLogger<HybridAiService>.Instance);

        (await sut.EmbedDocumentAsync(doc.Id)).Embedded.Should().BeTrue();
        var firstCalls = calls;
        firstCalls.Should().BeGreaterThan(0);

        (await sut.EmbedDocumentAsync(doc.Id)).Embedded.Should().BeTrue();
        calls.Should().Be(firstCalls);
    }

    [Fact]
    public async Task ReindexAllEmbeddingsAsync_IndexesDocumentsAndKnowledge()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        db.Documents.Add(new Document
        {
            Id = Guid.NewGuid(),
            FileName = "a.pdf",
            StoragePath = "p/a.pdf",
            FullTextContent = "reindex me document",
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.KnowledgeEntries.Add(new KnowledgeEntry
        {
            Id = Guid.NewGuid(),
            Title = "How to reindex",
            Content = "reindex me vault",
            Category = KnowledgeCategory.HowTo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactoryWithEmbedder(text => Vector(text));
        var sut = new HybridAiService(db, ollama, DisabledGrok, Mock.Of<IFileStorageService>(), Mock.Of<IDocumentAgentExtractionBackend>(), NullLogger<HybridAiService>.Instance);

        var result = await sut.ReindexAllEmbeddingsAsync();
        result.DocumentsEmbedded.Should().Be(1);
        result.KnowledgeEmbedded.Should().Be(1);
        db.EmbeddingChunks.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SemanticSearchDocumentsAsync_EmptyQuery_ReturnsEmpty()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = new HybridAiService(db, CreateOllamaFactoryWithEmbedder(_ => Vector("x")), DisabledGrok, Mock.Of<IFileStorageService>(), Mock.Of<IDocumentAgentExtractionBackend>(), NullLogger<HybridAiService>.Instance);

        var response = await sut.SemanticSearchDocumentsAsync(new SemanticSearchRequest("   ", 3));
        response.Hits.Should().BeEmpty();
    }

    [Fact]
    public async Task EmbedDocumentAsync_ThrowsWhenDocumentMissing()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var ollama = CreateOllamaFactoryWithEmbedder(_ => Vector("x"));
        var sut = new HybridAiService(db, ollama, DisabledGrok, Mock.Of<IFileStorageService>(), Mock.Of<IDocumentAgentExtractionBackend>(), NullLogger<HybridAiService>.Instance);

        var act = async () => await sut.EmbedDocumentAsync(Guid.NewGuid());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task SemanticSearchDocumentsAsync_ClampsTopK()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        for (var i = 0; i < 5; i++)
            SeedWithEmbedding(db, $"doc{i}.pdf", $"budget content {i}");
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactoryWithEmbedder(text => Vector(text));
        var sut = new HybridAiService(db, ollama, DisabledGrok, Mock.Of<IFileStorageService>(), Mock.Of<IDocumentAgentExtractionBackend>(), NullLogger<HybridAiService>.Instance);

        var low = await sut.SemanticSearchDocumentsAsync(new SemanticSearchRequest("budget", 0, MinScore: 0));
        low.Hits.Should().HaveCountLessThanOrEqualTo(1);

        var high = await sut.SemanticSearchDocumentsAsync(new SemanticSearchRequest("budget", 100, MinScore: 0));
        high.Hits.Should().HaveCountLessThanOrEqualTo(20);
    }

    private const int VectorDimensions = 16;

    /// <summary>
    /// Maps text to a stable bag-of-words vector. Shared tokens produce a strong cosine signal
    /// between the query and a document, exercising the ranking path without a real embedder.
    /// </summary>
    private static float[] Vector(string text)
    {
        var vector = new float[VectorDimensions];
        var tokens = text.ToLowerInvariant().Split(
            new[] { ' ', '\n', '\r', '\t', '.', ',' },
            StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in tokens)
        {
            // FNV-1a: deterministic across processes/platforms, unlike string.GetHashCode()
            // which is randomized per AppDomain in .NET Core 2.1+ and can collide token
            // slots differently on Linux CI vs macOS local.
            var slot = StableHash(token) % VectorDimensions;
            vector[slot] += 1f;
        }
        return vector;
    }

    private static uint StableHash(string s)
    {
        const uint fnvOffsetBasis = 2166136261;
        const uint fnvPrime = 16777619;
        uint hash = fnvOffsetBasis;
        foreach (var c in s)
        {
            hash ^= c;
            hash *= fnvPrime;
        }
        return hash;
    }

    private static Document SeedWithEmbedding(Infrastructure.Data.TikrDbContext db, string fileName, string content)
    {
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            StoragePath = $"p/{fileName}",
            FullTextContent = content,
            Embedding = HybridAiService.PackFloats(Vector(content + " " + fileName)),
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Documents.Add(doc);
        return doc;
    }

    private static IOllamaChatClientFactory CreateOllamaFactoryWithEmbedder(Func<string, float[]?> embedder)
    {
        var factory = new Mock<IOllamaChatClientFactory>();
        factory.Setup(f => f.CreateChatClient()).Returns(new StubChatClient(""));
        factory.SetupGet(f => f.ChatModel).Returns("test-model");
        factory.Setup(f => f.CreateEmbeddingGenerator(It.IsAny<string>()))
            .Returns(new StubEmbeddingGenerator(embedder));
        return factory.Object;
    }
}
