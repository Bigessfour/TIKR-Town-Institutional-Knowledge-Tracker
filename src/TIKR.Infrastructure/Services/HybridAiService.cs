using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TIKR.Infrastructure.Data;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

public class HybridAiService(
    TikrDbContext db,
    IOllamaChatClientFactory ollamaFactory,
    GrokService grokService,
    IFileStorageService storage,
    IDocumentAgentExtractionBackend extractionBackend,
    ILogger<HybridAiService> logger) : IHybridAiService
{
    private const int TagPreviewChars = 4000;
    private const int MaxPersistedExtractChars = 100_000;
    internal const double DefaultMinScore = 0.38;
    private const double VectorWeight = 0.7;
    private const double KeywordWeight = 0.3;
    private const int PassageSnippetChars = 1000;

    public async Task<TagDocumentResponse> TagDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await db.Documents.FindAsync([documentId], cancellationToken)
            ?? throw new KeyNotFoundException($"Document {documentId} not found.");

        await TryBackfillFullTextAsync(document, cancellationToken);

        var previewSource = document.FullTextContent ?? document.FileName;
        var preview = previewSource[..Math.Min(TagPreviewChars, previewSource.Length)];
        var prompt = DocumentTagPromptBuilder.Build(document.FileName, preview);

        // Low temperature for deterministic JSON tagging; AskAdvanced keeps default sampling.
        var taggingOptions = new ChatOptions { Temperature = DocumentTagPromptBuilder.TaggingTemperature };
        var response = await GetLocalCompletionAsync(prompt, cancellationToken, taggingOptions);
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

        (tags, folder) = DocumentTagHeuristics.FillGaps(
            document.FileName,
            document.FullTextContent,
            tags,
            folder);

        document.AiTags = JsonSerializer.Serialize(tags);
        document.SuggestedFolder = folder;
        document.UpdatedAt = DateTime.UtcNow;

        // Best-effort: refresh chunk embeddings so semantic search stays current.
        // Never block tagging if the embedding model is unavailable.
        await TryIndexDocumentChunksAsync(document, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return new TagDocumentResponse(documentId, tags, folder);
    }

    private async Task TryBackfillFullTextAsync(Document document, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(document.FullTextContent))
            return;
        if (string.IsNullOrWhiteSpace(document.StoragePath))
            return;

        try
        {
            await using var stream = await storage.OpenReadAsync(document.StoragePath, cancellationToken);
            await using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, cancellationToken);
            buffer.Position = 0;

            var result = await extractionBackend.ExtractAsync(buffer, document.FileName, cancellationToken);
            if (!IsUsableExtractedText(result))
                return;

            var text = result.ExtractedText.Trim();
            if (text.Length > MaxPersistedExtractChars)
                text = text[..MaxPersistedExtractChars];

            document.FullTextContent = text;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to backfill FullTextContent for document {DocumentId}", document.Id);
        }
    }

    internal static bool IsUsableExtractedText(AgentExtractionResult result)
    {
        if (string.IsNullOrWhiteSpace(result.ExtractedText))
            return false;
        if (result.UsedSyncfusionTools)
            return true;
        // Stub backend returns a placeholder for PDF/DOCX — do not persist that as content.
        return !result.ExtractedText.StartsWith("Agent stub:", StringComparison.Ordinal);
    }

    public async Task<EmbedDocumentResponse> EmbedDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var document = await db.Documents.FindAsync([documentId], cancellationToken)
            ?? throw new KeyNotFoundException($"Document {documentId} not found.");

        var ok = await TryIndexDocumentChunksAsync(document, cancellationToken);
        if (!ok)
            return new EmbedDocumentResponse(documentId, false, "Embedding generator unavailable (is Ollama running with nomic-embed-text?)");

        document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return new EmbedDocumentResponse(documentId, true, null);
    }

    public async Task<SemanticSearchResponse> SemanticSearchDocumentsAsync(SemanticSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return new SemanticSearchResponse(request.Query, 0, [], EmbeddingAvailable: true);

        var topK = Math.Clamp(request.TopK, 1, 20);
        var minScore = request.MinScore ?? DefaultMinScore;

        var queryVector = await TryGenerateEmbeddingAsync(request.Query, cancellationToken);
        if (queryVector is null)
            return new SemanticSearchResponse(request.Query, 0, [], EmbeddingAvailable: false);

        var chunkQuery = db.EmbeddingChunks.Where(c => c.SourceType == EmbeddingSourceType.Document);
        if (!string.IsNullOrWhiteSpace(request.Folder))
            chunkQuery = chunkQuery.Where(c => c.Facet == request.Folder);

        var chunks = await chunkQuery.ToListAsync(cancellationToken);
        var transientIds = await db.Documents
            .Where(d => d.IsTransient)
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);
        var transientSet = transientIds.ToHashSet();
        chunks = chunks.Where(c => !transientSet.Contains(c.SourceId)).ToList();
        var chunkSourceIds = chunks.Select(c => c.SourceId).Distinct().ToHashSet();

        var chunkHits = RankChunks(chunks, request.Query, queryVector, minScore)
            .GroupBy(x => x.SourceId)
            .Select(g => g.OrderByDescending(x => x.Score).First())
            .Select(x => new SemanticSearchHit(
                x.SourceId,
                x.DisplayName ?? "document",
                x.Facet,
                BuildSnippet(x.Content, request.Query, PassageSnippetChars),
                x.Score,
                x.ChunkIndex));

        // Legacy whole-document vectors for sources not yet chunk-indexed.
        var docs = await db.Documents
            .Where(d => d.Embedding != null && !d.IsTransient)
            .Select(d => new { d.Id, d.FileName, d.SuggestedFolder, d.FullTextContent, d.Embedding })
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Folder))
            docs = docs.Where(d => d.SuggestedFolder == request.Folder).ToList();

        var legacyDocs = docs.Where(d => !chunkSourceIds.Contains(d.Id)).ToList();
        var legacyHits = legacyDocs
            .Select(d =>
            {
                var vec = UnpackFloats(d.Embedding!);
                var cosine = CosineSimilarity(queryVector, vec);
                var keyword = KeywordOverlap(request.Query, $"{d.FileName} {d.FullTextContent}");
                var score = BlendScore(cosine, keyword);
                var snippet = BuildSnippet(d.FullTextContent ?? d.FileName, request.Query, PassageSnippetChars);
                return new SemanticSearchHit(d.Id, d.FileName, d.SuggestedFolder, snippet, score);
            })
            .Where(h => h.Score >= minScore);

        var hits = chunkHits.Concat(legacyHits)
            .OrderByDescending(h => h.Score)
            .Take(topK)
            .ToList();

        var considered = chunkSourceIds.Count + legacyDocs.Count;
        return new SemanticSearchResponse(request.Query, considered, hits, EmbeddingAvailable: true);
    }

    public async Task<EmbedKnowledgeEntryResponse> EmbedKnowledgeEntryAsync(Guid entryId, CancellationToken cancellationToken = default)
    {
        var entry = await db.KnowledgeEntries.FindAsync([entryId], cancellationToken)
            ?? throw new KeyNotFoundException($"Knowledge entry {entryId} not found.");

        var ok = await TryIndexKnowledgeChunksAsync(entry, cancellationToken);
        if (!ok)
            return new EmbedKnowledgeEntryResponse(entryId, false, "Embedding generator unavailable (is Ollama running with nomic-embed-text?)");

        entry.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return new EmbedKnowledgeEntryResponse(entryId, true, null);
    }

    public async Task<SemanticSearchKnowledgeResponse> SemanticSearchKnowledgeAsync(SemanticSearchRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return new SemanticSearchKnowledgeResponse(request.Query, 0, [], EmbeddingAvailable: true);

        var topK = Math.Clamp(request.TopK, 1, 20);
        var minScore = request.MinScore ?? DefaultMinScore;

        var queryVector = await TryGenerateEmbeddingAsync(request.Query, cancellationToken);
        if (queryVector is null)
            return new SemanticSearchKnowledgeResponse(request.Query, 0, [], EmbeddingAvailable: false);

        var chunkQuery = db.EmbeddingChunks.Where(c => c.SourceType == EmbeddingSourceType.Knowledge);
        if (!string.IsNullOrWhiteSpace(request.Category))
            chunkQuery = chunkQuery.Where(c => c.Facet == request.Category);

        var chunks = await chunkQuery.ToListAsync(cancellationToken);
        var chunkSourceIds = chunks.Select(c => c.SourceId).Distinct().ToHashSet();

        var chunkHits = RankChunks(chunks, request.Query, queryVector, minScore)
            .GroupBy(x => x.SourceId)
            .Select(g => g.OrderByDescending(x => x.Score).First())
            .Select(x => new SemanticSearchKnowledgeHit(
                x.SourceId,
                x.DisplayName ?? "entry",
                x.Facet ?? "Unknown",
                BuildSnippet(x.Content, request.Query, PassageSnippetChars),
                x.Score,
                x.ChunkIndex));

        var entries = await db.KnowledgeEntries
            .Where(e => e.Embedding != null)
            .Select(e => new { e.Id, e.Title, e.Category, e.Content, e.Embedding })
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Category))
            entries = entries.Where(e => e.Category.ToString() == request.Category).ToList();

        var legacyEntries = entries.Where(e => !chunkSourceIds.Contains(e.Id)).ToList();
        var legacyHits = legacyEntries
            .Select(e =>
            {
                var vec = UnpackFloats(e.Embedding!);
                var cosine = CosineSimilarity(queryVector, vec);
                var keyword = KeywordOverlap(request.Query, $"{e.Title} {e.Content}");
                var score = BlendScore(cosine, keyword);
                var snippet = BuildSnippet(e.Content, request.Query, PassageSnippetChars);
                return new SemanticSearchKnowledgeHit(e.Id, e.Title, e.Category.ToString(), snippet, score);
            })
            .Where(h => h.Score >= minScore);

        var hits = chunkHits.Concat(legacyHits)
            .OrderByDescending(h => h.Score)
            .Take(topK)
            .ToList();

        var considered = chunkSourceIds.Count + legacyEntries.Count;
        return new SemanticSearchKnowledgeResponse(request.Query, considered, hits, EmbeddingAvailable: true);
    }

    public async Task<ReindexEmbeddingsResponse> ReindexAllEmbeddingsAsync(CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var docs = await db.Documents.ToListAsync(cancellationToken);
        var entries = await db.KnowledgeEntries.ToListAsync(cancellationToken);
        var docsOk = 0;
        var knowledgeOk = 0;

        foreach (var doc in docs)
        {
            try
            {
                if (await TryIndexDocumentChunksAsync(doc, cancellationToken))
                    docsOk++;
                else
                    errors.Add($"Document {doc.Id}: Embedding generator unavailable");
            }
            catch (Exception ex)
            {
                errors.Add($"Document {doc.Id}: {ex.Message}");
            }
        }

        foreach (var entry in entries)
        {
            try
            {
                if (await TryIndexKnowledgeChunksAsync(entry, cancellationToken))
                    knowledgeOk++;
                else
                    errors.Add($"Knowledge {entry.Id}: Embedding generator unavailable");
            }
            catch (Exception ex)
            {
                errors.Add($"Knowledge {entry.Id}: {ex.Message}");
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return new ReindexEmbeddingsResponse(docs.Count, docsOk, entries.Count, knowledgeOk, errors);
    }

    public async Task<CorpusHealthResponse> GetCorpusHealthAsync(CancellationToken cancellationToken = default)
    {
        var documents = await db.Documents
            .Select(d => new { d.Id, d.FileName, d.IsTransient, d.FullTextContent })
            .ToListAsync(cancellationToken);
        var knowledge = await db.KnowledgeEntries.Select(k => k.Id).ToListAsync(cancellationToken);

        var docChunkIds = await db.EmbeddingChunks
            .Where(c => c.SourceType == EmbeddingSourceType.Document)
            .Select(c => c.SourceId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var knowledgeChunkIds = await db.EmbeddingChunks
            .Where(c => c.SourceType == EmbeddingSourceType.Knowledge)
            .Select(c => c.SourceId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var docChunkSet = docChunkIds.ToHashSet();
        var knowledgeChunkSet = knowledgeChunkIds.ToHashSet();

        var recurring = documents.Where(d => !d.IsTransient).ToList();
        var withChunks = recurring.Count(d => docChunkSet.Contains(d.Id));
        var sparse = documents
            .Where(d => !d.IsTransient && IsSparseForEmbedding(d.FullTextContent))
            .Select(d => d.FileName)
            .OrderBy(n => n)
            .Take(25)
            .ToList();

        var knowledgeWithChunks = knowledge.Count(id => knowledgeChunkSet.Contains(id));
        var docPct = recurring.Count == 0 ? 100.0 : Math.Round(100.0 * withChunks / recurring.Count, 1);
        var knowledgePct = knowledge.Count == 0 ? 100.0 : Math.Round(100.0 * knowledgeWithChunks / knowledge.Count, 1);

        return new CorpusHealthResponse(
            DocumentsTotal: documents.Count,
            DocumentsWithChunks: withChunks,
            DocumentsTransient: documents.Count(d => d.IsTransient),
            DocumentsSparseText: sparse.Count,
            KnowledgeTotal: knowledge.Count,
            KnowledgeWithChunks: knowledgeWithChunks,
            DocumentsChunkCoveragePercent: docPct,
            KnowledgeChunkCoveragePercent: knowledgePct,
            NeedsAttention: sparse);
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
                priority,
                r.SubmitTo,
                r.ContactName,
                r.ContactEmail,
                r.ContactPhone);
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
        var prompt = string.IsNullOrWhiteSpace(request.Context)
            ? request.Prompt
            : $"Context:\n{request.Context}\n\nQuestion:\n{request.Prompt}";

        // Validate Ollama first per requirements. Use local unless unavailable or prompt context requires advanced/Grok reasoning.
        bool ollamaAvailable = false;
        try
        {
            ollamaAvailable = await ollamaFactory.IsAvailableAsync(cancellationToken);
        }
        catch { /* best effort */ }

        bool preferGrokByContext = ShouldPreferGrokForPrompt(prompt);

        if (!ollamaAvailable || (preferGrokByContext && grokService.IsEnabled))
        {
            if (grokService.IsEnabled)
            {
                var grokAnswer = await grokService.CompleteAsync(prompt, cancellationToken: cancellationToken);
                if (!string.IsNullOrWhiteSpace(grokAnswer))
                    return new AskAdvancedResponse(grokAnswer, UsedGrok: true);
            }
        }

        // Prefer local (validated) first
        var localAnswer = await GetLocalCompletionAsync(prompt, cancellationToken);
        if (!string.IsNullOrWhiteSpace(localAnswer))
            return new AskAdvancedResponse(localAnswer, UsedGrok: false);

        // Fallback to Grok if local failed and Grok enabled (even if not preferred by context)
        if (grokService.IsEnabled)
        {
            var grokAnswer = await grokService.CompleteAsync(prompt, cancellationToken: cancellationToken);
            if (!string.IsNullOrWhiteSpace(grokAnswer))
                return new AskAdvancedResponse(grokAnswer, UsedGrok: true);
        }

        return new AskAdvancedResponse("Unable to get a response. Check Ollama connectivity (or enable USE_GROK with a valid GROK_API_KEY).", UsedGrok: false);
    }

    private static bool ShouldPreferGrokForPrompt(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return false;
        var p = prompt.ToLowerInvariant();
        // Context-dependent: trigger Grok for explicit advanced/complex reasoning prompts
        return p.Contains("grok") ||
               p.Contains("advanced") ||
               p.Contains("complex reasoning") ||
               p.Contains("deep analysis") ||
               p.Contains("detailed step") ||
               (p.Contains("step by step") && p.Length > 80) ||
               p.Contains("thorough explanation");
    }

    public async Task<AiStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var ollamaAvailable = await ollamaFactory.IsAvailableAsync(cancellationToken);
        return new AiStatusResponse(ollamaAvailable, ollamaFactory.ChatModel, grokService.IsEnabled);
    }

    private async Task<string?> GetLocalCompletionAsync(
        string prompt,
        CancellationToken cancellationToken,
        ChatOptions? options = null)
    {
        try
        {
            var client = ollamaFactory.CreateChatClient();
            var response = await client.GetResponseAsync(prompt, options, cancellationToken);
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
        if (string.IsNullOrWhiteSpace(text)) return text;
        // Strip common markdown code fences for robustness
        var cleaned = text.Trim();
        if (cleaned.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNl = cleaned.IndexOf('\n');
            if (firstNl > 0) cleaned = cleaned[(firstNl + 1)..];
            var lastFence = cleaned.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence > 0) cleaned = cleaned[..lastFence];
            cleaned = cleaned.Trim();
        }
        var start = cleaned.IndexOf('{');
        var end = cleaned.LastIndexOf('}');
        return start >= 0 && end > start ? cleaned[start..(end + 1)] : cleaned;
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

    internal static string BuildEmbeddingText(Document document)
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

    private static string BuildKnowledgeEmbeddingText(KnowledgeEntry entry)
    {
        // Category gives the embedder useful framing (HowTo vs Contacts vs Tribal vs VoiceNotes).
        return $"{entry.Category}: {entry.Title}\n{entry.Content}";
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

    internal static double KeywordOverlap(string query, string text)
    {
        var qTokens = Tokenize(query);
        if (qTokens.Count == 0) return 0;
        var tTokens = Tokenize(text);
        if (tTokens.Count == 0) return 0;
        var overlap = qTokens.Count(t => tTokens.Contains(t));
        return (double)overlap / qTokens.Count;
    }

    internal static double BlendScore(double cosine, double keyword) =>
        (VectorWeight * cosine) + (KeywordWeight * keyword);

    /// <summary>Matches SyncfusionDocumentOcrService.NeedsOcr letter threshold for embed completeness gate.</summary>
    internal static bool IsSparseForEmbedding(string? text, int minLetterChars = 48) =>
        string.IsNullOrWhiteSpace(text) || text.Count(char.IsLetter) < minLetterChars;

    private static HashSet<string> Tokenize(string text) =>
        text.ToLowerInvariant()
            .Split([' ', '\n', '\r', '\t', '.', ',', ';', ':', '/', '-', '_', '(', ')'], StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 1)
            .ToHashSet(StringComparer.Ordinal);

    private async Task<bool> TryIndexDocumentChunksAsync(Document document, CancellationToken cancellationToken)
    {
        if (document.IsTransient)
            return true; // Transitory filings are kept on disk but excluded from long-term Assistant RAG.

        var sourceText = BuildEmbeddingText(document);
        if (string.IsNullOrWhiteSpace(sourceText) || IsSparseForEmbedding(sourceText))
        {
            // Sparse / missing text: do not pretend the corpus is complete until OCR/backfill succeeds.
            return false;
        }

        var sourceHash = TextChunker.Sha256Hex(sourceText);
        var existing = await db.EmbeddingChunks
            .Where(c => c.SourceType == EmbeddingSourceType.Document && c.SourceId == document.Id)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0 && existing.All(c => c.ContentHash == sourceHash))
            return true;

        var passages = TextChunker.Chunk(sourceText);
        if (passages.Count == 0)
            return false;

        var vectors = new List<float[]>();
        foreach (var passage in passages)
        {
            var vector = await TryGenerateEmbeddingAsync(passage, cancellationToken);
            if (vector is null)
                return false;
            vectors.Add(vector);
        }

        if (existing.Count > 0)
            db.EmbeddingChunks.RemoveRange(existing);

        for (var i = 0; i < passages.Count; i++)
        {
            db.EmbeddingChunks.Add(new EmbeddingChunk
            {
                Id = Guid.NewGuid(),
                SourceType = EmbeddingSourceType.Document,
                SourceId = document.Id,
                ChunkIndex = i,
                Content = passages[i],
                Embedding = PackFloats(vectors[i]),
                ContentHash = sourceHash,
                DisplayName = document.FileName,
                Facet = document.SuggestedFolder,
                UpdatedAt = DateTime.UtcNow
            });
        }

        // Legacy summary vector: first chunk (keeps older consumers / diagnostics working).
        document.Embedding = PackFloats(vectors[0]);
        return true;
    }

    private async Task<bool> TryIndexKnowledgeChunksAsync(KnowledgeEntry entry, CancellationToken cancellationToken)
    {
        var sourceText = BuildKnowledgeEmbeddingText(entry);
        if (string.IsNullOrWhiteSpace(sourceText))
            return false;

        var sourceHash = TextChunker.Sha256Hex(sourceText);
        var existing = await db.EmbeddingChunks
            .Where(c => c.SourceType == EmbeddingSourceType.Knowledge && c.SourceId == entry.Id)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0 && existing.All(c => c.ContentHash == sourceHash))
            return true;

        var passages = TextChunker.Chunk(sourceText);
        if (passages.Count == 0)
            return false;

        var vectors = new List<float[]>();
        foreach (var passage in passages)
        {
            var vector = await TryGenerateEmbeddingAsync(passage, cancellationToken);
            if (vector is null)
                return false;
            vectors.Add(vector);
        }

        if (existing.Count > 0)
            db.EmbeddingChunks.RemoveRange(existing);

        var facet = entry.Category.ToString();
        for (var i = 0; i < passages.Count; i++)
        {
            db.EmbeddingChunks.Add(new EmbeddingChunk
            {
                Id = Guid.NewGuid(),
                SourceType = EmbeddingSourceType.Knowledge,
                SourceId = entry.Id,
                ChunkIndex = i,
                Content = passages[i],
                Embedding = PackFloats(vectors[i]),
                ContentHash = sourceHash,
                DisplayName = entry.Title,
                Facet = facet,
                UpdatedAt = DateTime.UtcNow
            });
        }

        entry.Embedding = PackFloats(vectors[0]);
        return true;
    }

    private static IEnumerable<(Guid SourceId, string? DisplayName, string? Facet, string Content, int ChunkIndex, double Score)> RankChunks(
        IReadOnlyList<EmbeddingChunk> chunks,
        string query,
        float[] queryVector,
        double minScore)
    {
        foreach (var chunk in chunks)
        {
            var cosine = CosineSimilarity(queryVector, UnpackFloats(chunk.Embedding));
            var keyword = KeywordOverlap(query, $"{chunk.DisplayName} {chunk.Content}");
            var score = BlendScore(cosine, keyword);
            if (score < minScore)
                continue;
            yield return (chunk.SourceId, chunk.DisplayName, chunk.Facet, chunk.Content, chunk.ChunkIndex, score);
        }
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
