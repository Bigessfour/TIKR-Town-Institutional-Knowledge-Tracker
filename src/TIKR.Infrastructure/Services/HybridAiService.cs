using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TIKR.Infrastructure.Data;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

public class HybridAiService(
    TikrDbContext db,
    IOllamaChatClientFactory ollamaFactory,
    GrokService grokService,
    ILogger<HybridAiService> logger) : IHybridAiService
{
    public async Task<TagDocumentResponse> TagDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await db.Documents.FindAsync([documentId], cancellationToken)
            ?? throw new KeyNotFoundException($"Document {documentId} not found.");

        var preview = (document.FullTextContent ?? document.FileName);
        preview = preview[..Math.Min(500, preview.Length)];
        var prompt =
            "Analyze this municipal document and respond with JSON only: " +
            "{\"tags\": [\"tag1\",\"tag2\"], \"suggestedFolder\": \"folder name\"}\n\n" +
            $"File name: {document.FileName}\nContent preview: {preview}";

        var response = await GetLocalCompletionAsync(prompt, cancellationToken);
        var tags = Array.Empty<string>();
        string? folder = null;

        if (!string.IsNullOrWhiteSpace(response))
        {
            try
            {
                using var doc = JsonDocument.Parse(ExtractJson(response));
                if (doc.RootElement.TryGetProperty("tags", out var tagsEl))
                    tags = tagsEl.EnumerateArray().Select(t => t.GetString() ?? "").Where(t => t.Length > 0).ToArray();
                if (doc.RootElement.TryGetProperty("suggestedFolder", out var folderEl))
                    folder = folderEl.GetString();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to parse AI tagging response");
                tags = ["uncategorized"];
            }
        }

        document.AiTags = JsonSerializer.Serialize(tags);
        document.SuggestedFolder = folder;
        document.UpdatedAt = DateTime.UtcNow;

        // Best-effort: refresh embedding from the just-tagged content so semantic search stays current.
        // Never block tagging if the embedding model is unavailable.
        var embedding = await TryGenerateEmbeddingAsync(BuildEmbeddingText(document), cancellationToken);
        if (embedding is not null)
            document.Embedding = PackFloats(embedding);

        await db.SaveChangesAsync(cancellationToken);

        return new TagDocumentResponse(documentId, tags, folder);
    }

    public async Task<EmbedDocumentResponse> EmbedDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await db.Documents.FindAsync([documentId], cancellationToken)
            ?? throw new KeyNotFoundException($"Document {documentId} not found.");

        var text = BuildEmbeddingText(document);
        var vector = await TryGenerateEmbeddingAsync(text, cancellationToken);
        if (vector is null)
            return new EmbedDocumentResponse(documentId, false, "Embedding generator unavailable (is Ollama running with nomic-embed-text?)");

        document.Embedding = PackFloats(vector);
        document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return new EmbedDocumentResponse(documentId, true, null);
    }

    public async Task<SemanticSearchResponse> SemanticSearchDocumentsAsync(SemanticSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return new SemanticSearchResponse(request.Query, 0, []);

        var topK = Math.Clamp(request.TopK, 1, 20);

        var queryVector = await TryGenerateEmbeddingAsync(request.Query, cancellationToken);
        if (queryVector is null)
            return new SemanticSearchResponse(request.Query, 0, []);

        // Embeddings live on Documents; pull only rows with an embedding so we don't ship raw bytes for nothing.
        var docs = await db.Documents
            .Where(d => d.Embedding != null)
            .Select(d => new { d.Id, d.FileName, d.SuggestedFolder, d.FullTextContent, d.Embedding })
            .ToListAsync(cancellationToken);

        var hits = docs
            .Select(d =>
            {
                var vec = UnpackFloats(d.Embedding!);
                var score = CosineSimilarity(queryVector, vec);
                var snippet = BuildSnippet(d.FullTextContent ?? d.FileName, request.Query, 240);
                return new SemanticSearchHit(d.Id, d.FileName, d.SuggestedFolder, snippet, score);
            })
            .OrderByDescending(h => h.Score)
            .Take(topK)
            .ToList();

        return new SemanticSearchResponse(request.Query, docs.Count, hits);
    }

    public async Task<IReadOnlyList<DashboardPriority>> GetDashboardPrioritiesAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var upcoming = await db.Requirements
            .Where(r => !r.IsCompleted && r.DueDate >= today.AddDays(-30))
            .OrderBy(r => r.DueDate)
            .Take(10)
            .ToListAsync(cancellationToken);

        var priorities = upcoming.Select(r =>
        {
            var daysUntil = r.DueDate.DayNumber - today.DayNumber;
            var priority = daysUntil < 0 ? "Overdue" : daysUntil <= 14 ? "High" : daysUntil <= 30 ? "Medium" : "Low";
            return new DashboardPriority(
                r.Title,
                r.Description ?? $"Due in {daysUntil} days",
                r.DueDate,
                priority);
        }).ToList();

        if (priorities.Count == 0)
        {
            priorities.Add(new DashboardPriority(
                "No urgent deadlines",
                "Add custom requirements or review the calendar.",
                null,
                "Low"));
        }

        return priorities;
    }

    public async Task<AskAdvancedResponse> AskAdvancedAsync(AskAdvancedRequest request, CancellationToken cancellationToken = default)
    {
        if (!grokService.IsEnabled)
            throw new InvalidOperationException("Advanced AI (Grok) is not enabled. Set USE_GROK=true and provide GROK_API_KEY.");

        var prompt = string.IsNullOrWhiteSpace(request.Context)
            ? request.Prompt
            : $"Context:\n{request.Context}\n\nQuestion:\n{request.Prompt}";

        var answer = await grokService.CompleteAsync(prompt, cancellationToken: cancellationToken)
            ?? "Unable to get a response from Grok. Check your API key and network connection.";

        return new AskAdvancedResponse(answer, UsedGrok: true);
    }

    public async Task<AiStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var ollamaAvailable = await ollamaFactory.IsAvailableAsync(cancellationToken);
        return new AiStatusResponse(ollamaAvailable, ollamaFactory.ChatModel, grokService.IsEnabled);
    }

    private async Task<string?> GetLocalCompletionAsync(string prompt, CancellationToken cancellationToken)
    {
        try
        {
            var client = ollamaFactory.CreateChatClient();
            var response = await client.GetResponseAsync(prompt, cancellationToken: cancellationToken);
            return response.Text;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Ollama completion failed");
            return null;
        }
    }

    private static string ExtractJson(string text)
    {
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : text;
    }

    private async Task<float[]?> TryGenerateEmbeddingAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        try
        {
            var generator = ollamaFactory.CreateEmbeddingGenerator();
            // nomic-embed-text has an 8k context; we cap defensively for chat-model parity.
            var trimmed = text.Length > 4000 ? text[..4000] : text;
            var result = await generator.GenerateAsync([trimmed], cancellationToken: cancellationToken);
            var first = result.FirstOrDefault();
            return first?.Vector.ToArray();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Embedding generation failed (Ollama may be offline or model missing)");
            return null;
        }
    }

    private static string BuildEmbeddingText(Document document)
    {
        var parts = new List<string> { document.FileName };
        if (!string.IsNullOrWhiteSpace(document.SuggestedFolder))
            parts.Add(document.SuggestedFolder);
        if (!string.IsNullOrWhiteSpace(document.AiTags))
            parts.Add(document.AiTags);
        if (!string.IsNullOrWhiteSpace(document.FullTextContent))
            parts.Add(document.FullTextContent);
        return string.Join("\n", parts);
    }

    internal static byte[] PackFloats(float[] vector)
    {
        var bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    internal static float[] UnpackFloats(byte[] bytes)
    {
        var vector = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, vector, 0, bytes.Length);
        return vector;
    }

    internal static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length == 0 || b.Length == 0 || a.Length != b.Length)
            return 0;

        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        if (normA == 0 || normB == 0) return 0;
        return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
    }

    private static string? BuildSnippet(string text, string query, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var firstWord = query.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        var idx = !string.IsNullOrEmpty(firstWord)
            ? text.IndexOf(firstWord, StringComparison.OrdinalIgnoreCase)
            : -1;
        var start = idx < 0 ? 0 : Math.Max(0, idx - 40);
        var len = Math.Min(maxLen, text.Length - start);
        var snippet = text.Substring(start, len).Trim();
        return start > 0 ? "…" + snippet : snippet;
    }
}
