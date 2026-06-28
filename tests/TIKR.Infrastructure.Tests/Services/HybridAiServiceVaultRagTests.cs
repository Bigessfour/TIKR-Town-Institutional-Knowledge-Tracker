using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TIKR.Infrastructure.Data;
using TIKR.Infrastructure.Services;
using TIKR.Infrastructure.Tests.Helpers;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;

namespace TIKR.Infrastructure.Tests.Services;

public class HybridAiServiceVaultRagTests
{
    private static readonly GrokService DisabledGrok = TestGrokServiceFactory.CreateDisabled();

    [Fact]
    public async Task EmbedKnowledgeEntryAsync_StoresEmbeddingBytes()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var entry = new KnowledgeEntry
        {
            Id = Guid.NewGuid(),
            Title = "How to issue a building permit",
            Content = "Open the permit register, log the address, charge the fee, hand off to inspector.",
            Category = KnowledgeCategory.HowTo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.KnowledgeEntries.Add(entry);
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactoryWithEmbedder(text => Vector(text));
        var sut = new HybridAiService(db, ollama, DisabledGrok, NullLogger<HybridAiService>.Instance);

        var result = await sut.EmbedKnowledgeEntryAsync(entry.Id);
        result.Embedded.Should().BeTrue();

        var reloaded = await db.KnowledgeEntries.FindAsync(entry.Id);
        reloaded!.Embedding.Should().NotBeNull();
        reloaded.Embedding!.Length.Should().Be(VectorDimensions * sizeof(float));
    }

    [Fact]
    public async Task EmbedKnowledgeEntryAsync_ThrowsWhenEntryMissing()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var ollama = CreateOllamaFactoryWithEmbedder(_ => Vector("x"));
        var sut = new HybridAiService(db, ollama, DisabledGrok, NullLogger<HybridAiService>.Instance);

        var act = async () => await sut.EmbedKnowledgeEntryAsync(Guid.NewGuid());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task EmbedKnowledgeEntryAsync_ReturnsFailureWhenGeneratorThrows()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var entry = SeedEntry(db, "Note", "content", KnowledgeCategory.TribalKnowledge);
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactoryWithEmbedder(_ => null);
        var sut = new HybridAiService(db, ollama, DisabledGrok, NullLogger<HybridAiService>.Instance);

        var result = await sut.EmbedKnowledgeEntryAsync(entry.Id);
        result.Embedded.Should().BeFalse();
        result.Reason.Should().NotBeNullOrWhiteSpace();

        var reloaded = await db.KnowledgeEntries.FindAsync(entry.Id);
        reloaded!.Embedding.Should().BeNull();
    }

    [Fact]
    public async Task SemanticSearchKnowledgeAsync_RanksByCosineSimilarity()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var permits = SeedEntryWithEmbedding(db, "Issue building permit", "permit register fee inspector", KnowledgeCategory.HowTo);
        var contacts = SeedEntryWithEmbedding(db, "Town attorney contact", "Sarah Smith phone email", KnowledgeCategory.Contact);
        var tribal = SeedEntryWithEmbedding(db, "Where files live", "filing cabinet third drawer back", KnowledgeCategory.TribalKnowledge);
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactoryWithEmbedder(text => Vector(text));
        var sut = new HybridAiService(db, ollama, DisabledGrok, NullLogger<HybridAiService>.Instance);

        var response = await sut.SemanticSearchKnowledgeAsync(new SemanticSearchRequest("permit fee", 2));

        response.Considered.Should().Be(3);
        response.Hits.Should().HaveCount(2);
        response.Hits[0].EntryId.Should().Be(permits.Id);
        response.Hits[0].Category.Should().Be("HowTo");
        response.Hits[0].Score.Should().BeGreaterThan(response.Hits[1].Score);
    }

    [Fact]
    public async Task SemanticSearchKnowledgeAsync_SkipsEntriesWithoutEmbedding()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        SeedEntryWithEmbedding(db, "With embedding", "permits fees", KnowledgeCategory.HowTo);
        SeedEntry(db, "Without embedding", "permits fees", KnowledgeCategory.HowTo);
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactoryWithEmbedder(text => Vector(text));
        var sut = new HybridAiService(db, ollama, DisabledGrok, NullLogger<HybridAiService>.Instance);

        var response = await sut.SemanticSearchKnowledgeAsync(new SemanticSearchRequest("permit", 5));
        response.Considered.Should().Be(1);
        response.Hits.Should().ContainSingle()
            .Which.Title.Should().Be("With embedding");
    }

    [Fact]
    public async Task SemanticSearchKnowledgeAsync_ReturnsEmptyWhenGeneratorFails()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        SeedEntryWithEmbedding(db, "any", "content", KnowledgeCategory.HowTo);
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactoryWithEmbedder(_ => null);
        var sut = new HybridAiService(db, ollama, DisabledGrok, NullLogger<HybridAiService>.Instance);

        var response = await sut.SemanticSearchKnowledgeAsync(new SemanticSearchRequest("query", 3));
        response.Hits.Should().BeEmpty();
    }

    [Fact]
    public async Task SemanticSearchKnowledgeAsync_EmptyQuery_ReturnsEmpty()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var ollama = CreateOllamaFactoryWithEmbedder(_ => Vector("x"));
        var sut = new HybridAiService(db, ollama, DisabledGrok, NullLogger<HybridAiService>.Instance);

        var response = await sut.SemanticSearchKnowledgeAsync(new SemanticSearchRequest("   ", 3));
        response.Hits.Should().BeEmpty();
    }

    [Fact]
    public async Task SemanticSearchKnowledgeAsync_ClampsTopK()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        for (var i = 0; i < 5; i++)
            SeedEntryWithEmbedding(db, $"Entry {i}", $"permit content {i}", KnowledgeCategory.HowTo);
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactoryWithEmbedder(text => Vector(text));
        var sut = new HybridAiService(db, ollama, DisabledGrok, NullLogger<HybridAiService>.Instance);

        var low = await sut.SemanticSearchKnowledgeAsync(new SemanticSearchRequest("permit", 0));
        low.Hits.Should().HaveCountLessThanOrEqualTo(1);

        var high = await sut.SemanticSearchKnowledgeAsync(new SemanticSearchRequest("permit", 100));
        high.Hits.Should().HaveCountLessThanOrEqualTo(20);
    }

    private const int VectorDimensions = 16;

    private static float[] Vector(string text)
    {
        var vector = new float[VectorDimensions];
        var tokens = text.ToLowerInvariant().Split(
            new[] { ' ', '\n', '\r', '\t', '.', ',', ':' },
            StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in tokens)
        {
            // FNV-1a: deterministic across processes/platforms, unlike string.GetHashCode()
            // which is randomized per AppDomain in .NET Core 2.1+ and caused this test to
            // collide token slots differently on Linux CI vs macOS local.
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

    private static KnowledgeEntry SeedEntry(TikrDbContext db, string title, string content, KnowledgeCategory category)
    {
        var entry = new KnowledgeEntry
        {
            Id = Guid.NewGuid(),
            Title = title,
            Content = content,
            Category = category,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.KnowledgeEntries.Add(entry);
        return entry;
    }

    private static KnowledgeEntry SeedEntryWithEmbedding(TikrDbContext db, string title, string content, KnowledgeCategory category)
    {
        var entry = SeedEntry(db, title, content, category);
        entry.Embedding = HybridAiService.PackFloats(Vector($"{category}: {title}\n{content}"));
        return entry;
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
