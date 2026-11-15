using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;

namespace TIKR.Infrastructure.Services;

public sealed class ChatTurnMemory(TikrDbContext db, ITextEmbedder embedder) : IChatTurnMemory
{
    public async Task IndexTurnAsync(
        string userId,
        Guid conversationId,
        string userText,
        string assistantText,
        CancellationToken ct = default)
    {
        var text = $"User: {userText.Trim()}\nAssistant: {assistantText.Trim()}".Trim();
        if (text.Length < 8)
            return;

        var passages = TextChunker.Chunk(text);
        if (passages.Count == 0)
            return;

        var vectors = new List<float[]>(passages.Count);
        foreach (var passage in passages)
        {
            var vector = await embedder.EmbedAsync(passage, ct);
            if (vector is null || vector.Length == 0)
                return;
            vectors.Add(vector);
        }

        var nextIndex = await db.EmbeddingChunks
            .Where(c => c.SourceType == EmbeddingSourceType.ChatTurn && c.SourceId == conversationId)
            .Select(c => (int?)c.ChunkIndex)
            .MaxAsync(ct) ?? -1;

        var hash = TextChunker.Sha256Hex(text);
        for (var i = 0; i < passages.Count; i++)
        {
            db.EmbeddingChunks.Add(new EmbeddingChunk
            {
                Id = Guid.NewGuid(),
                SourceType = EmbeddingSourceType.ChatTurn,
                SourceId = conversationId,
                ChunkIndex = nextIndex + 1 + i,
                Content = passages[i],
                Embedding = HybridAiService.PackFloats(vectors[i]),
                ContentHash = hash,
                DisplayName = "Chat",
                Facet = userId,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteConversationAsync(string userId, Guid conversationId, CancellationToken ct = default)
    {
        var rows = await db.EmbeddingChunks
            .Where(c => c.SourceType == EmbeddingSourceType.ChatTurn
                        && c.SourceId == conversationId
                        && c.Facet == userId)
            .ToListAsync(ct);
        if (rows.Count == 0)
            return;

        db.EmbeddingChunks.RemoveRange(rows);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<string>> SearchAsync(
        string userId,
        string query,
        int topK = 3,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var queryVector = await embedder.EmbedAsync(query, ct);
        if (queryVector is null || queryVector.Length == 0)
            return [];

        var chunks = await db.EmbeddingChunks
            .AsNoTracking()
            .Where(c => c.SourceType == EmbeddingSourceType.ChatTurn && c.Facet == userId)
            .ToListAsync(ct);

        return chunks
            .Select(chunk => (chunk.Content, Score: HybridAiService.CosineSimilarity(queryVector, HybridAiService.UnpackFloats(chunk.Embedding))))
            .Where(x => x.Score >= 0.2 && !string.IsNullOrWhiteSpace(x.Content))
            .OrderByDescending(x => x.Score)
            .Take(Math.Clamp(topK, 1, 8))
            .Select(x => x.Content)
            .ToList();
    }
}

public interface ITextEmbedder
{
    Task<float[]?> EmbedAsync(string text, CancellationToken ct = default);
}

public sealed class OllamaTextEmbedder(IOllamaChatClientFactory ollama) : ITextEmbedder
{
    public async Task<float[]?> EmbedAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            var trimmed = text.Length > 4000 ? text[..4000] : text;
            var generator = ollama.CreateEmbeddingGenerator();
            var result = await generator.GenerateAsync([trimmed], cancellationToken: ct);
            return result.FirstOrDefault()?.Vector.ToArray();
        }
        catch
        {
            return null;
        }
    }
}
