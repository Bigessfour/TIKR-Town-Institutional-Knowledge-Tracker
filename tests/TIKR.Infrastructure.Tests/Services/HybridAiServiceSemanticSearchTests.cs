using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TIKR.Infrastructure.Services;
using TIKR.Infrastructure.Tests.Helpers;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;

namespace TIKR.Infrastructure.Tests.Services;

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
        var sut = new HybridAiService(db, ollama, DisabledGrok, NullLogger<HybridAiService>.Instance);

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
        var sut = new HybridAiService(db, ollama, DisabledGrok, NullLogger<HybridAiService>.Instance);

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
        var sut = new HybridAiService(db, ollama, DisabledGrok, NullLogger<HybridAiService>.Instance);

        var response = await sut.SemanticSearchDocumentsAsync(new SemanticSearchRequest("budget", 2));

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
        var sut = new HybridAiService(db, ollama, DisabledGrok, NullLogger<HybridAiService>.Instance);

        var response = await sut.SemanticSearchDocumentsAsync(new SemanticSearchRequest("budget", 5));
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
        var sut = new HybridAiService(db, ollama, DisabledGrok, NullLogger<HybridAiService>.Instance);

        var response = await sut.SemanticSearchDocumentsAsync(new SemanticSearchRequest("query", 3));
        response.Hits.Should().BeEmpty();
    }

    [Fact]
    public async Task SemanticSearchDocumentsAsync_EmptyQuery_ReturnsEmpty()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = new HybridAiService(db, CreateOllamaFactoryWithEmbedder(_ => Vector("x")), DisabledGrok, NullLogger<HybridAiService>.Instance);

        var response = await sut.SemanticSearchDocumentsAsync(new SemanticSearchRequest("   ", 3));
        response.Hits.Should().BeEmpty();
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
            var slot = (uint)token.GetHashCode() % VectorDimensions;
            vector[slot] += 1f;
        }
        return vector;
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
