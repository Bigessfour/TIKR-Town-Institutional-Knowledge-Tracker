using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Diagnostics;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;
using TIKR.Shared.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

public class HybridAiService(
    TikrDbContext db,
    IOllamaChatClientFactory ollamaFactory,
    GrokService grokService,
    IFileStorageService storage,
    IDocumentAgentExtractionBackend extractionBackend,
    ILogger<HybridAiService> logger,
    FeatureSettingsState? featureSettings = null) : IHybridAiService
{
    private const int TagPreviewChars = 4000;
    private const int MaxPersistedExtractChars = 100_000;
    internal const double DefaultMinScore = 0.38;
    private const double VectorWeight = 0.7;
    private const double KeywordWeight = 0.3;
    private const int PassageSnippetChars = 1000;

    public async Task<TagDocumentResponse> TagDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        TikrActionLog.Started(logger, "AI.TagDocument", $"DocumentId={documentId}");

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

        TikrActionLog.Completed(logger, "AI.TagDocument",
            $"DocumentId={documentId} FileName={document.FileName} Tags={tags.Length} Folder={folder ?? "(none)"}",
            sw.ElapsedMilliseconds);
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
        var sw = Stopwatch.StartNew();
        TikrActionLog.Started(logger, "AI.EmbedDocument", $"DocumentId={documentId}");

        var document = await db.Documents.FindAsync([documentId], cancellationToken)
            ?? throw new KeyNotFoundException($"Document {documentId} not found.");

        var ok = await TryIndexDocumentChunksAsync(document, cancellationToken);
        if (!ok)
        {
            TikrActionLog.Failed(logger, "AI.EmbedDocument", "Embedding generator unavailable", $"DocumentId={documentId}");
            return new EmbedDocumentResponse(documentId, false, "Embedding generator unavailable (is Ollama running with nomic-embed-text?)");
        }

        document.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        TikrActionLog.Completed(logger, "AI.EmbedDocument", $"DocumentId={documentId} FileName={document.FileName}", sw.ElapsedMilliseconds);
        return new EmbedDocumentResponse(documentId, true, null);
    }

    public async Task<SemanticSearchResponse> SemanticSearchDocumentsAsync(SemanticSearchRequest request, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        TikrActionLog.Started(logger, "AI.SemanticSearchDocuments",
            $"QueryLen={request.Query?.Length ?? 0} TopK={request.TopK} Folder={request.Folder ?? "(any)"}");

        if (string.IsNullOrWhiteSpace(request.Query))
            return new SemanticSearchResponse(request.Query ?? string.Empty, 0, [], EmbeddingAvailable: true);

        var topK = Math.Clamp(request.TopK, 1, 20);
        var minScore = request.MinScore ?? DefaultMinScore;

        var queryVector = await TryGenerateEmbeddingAsync(request.Query, cancellationToken);
        if (queryVector is null)
        {
            TikrActionLog.Failed(logger, "AI.SemanticSearchDocuments", "Embeddings unavailable (Ollama/nomic-embed-text)");
            return new SemanticSearchResponse(request.Query, 0, [], EmbeddingAvailable: false);
        }

        var chunkQuery = db.EmbeddingChunks.Where(c => c.SourceType == EmbeddingSourceType.Document);
        if (!string.IsNullOrWhiteSpace(request.Folder))
            chunkQuery = chunkQuery.Where(c => c.Facet == request.Folder);

        var chunks = await chunkQuery.ToListAsync(cancellationToken);
        // Exclude transitory filings and soft-deleted (recycle bin) documents from Assistant RAG.
        var excludedIds = await db.Documents
            .Where(d => d.IsTransient || d.DeletedAt != null)
            .Select(d => d.Id)
            .ToListAsync(cancellationToken);
        var excludedSet = excludedIds.ToHashSet();
        chunks = chunks.Where(c => !excludedSet.Contains(c.SourceId)).ToList();
        var chunkSourceIds = chunks.Select(c => c.SourceId).Distinct().ToHashSet();

        var rankedChunks = RankChunks(chunks, request.Query, queryVector, minScore)
            .GroupBy(x => x.SourceId)
            .Select(g => g.OrderByDescending(x => x.Score).First())
            .ToList();

        // Load source metadata so agent labels use content topics even when DisplayName is a bare file name.
        var rankedSourceIds = rankedChunks.Select(x => x.SourceId).Distinct().ToList();
        var docMetaById = await db.Documents
            .Where(d => rankedSourceIds.Contains(d.Id) && d.DeletedAt == null)
            .Select(d => new { d.Id, d.FileName, d.SuggestedFolder, d.FullTextContent, d.AiTags })
            .ToDictionaryAsync(d => d.Id, cancellationToken);

        var chunkHits = rankedChunks
            .Where(x => docMetaById.ContainsKey(x.SourceId))
            .Select(x =>
        {
            docMetaById.TryGetValue(x.SourceId, out var meta);
            var fileName = meta?.FileName
                ?? StripTopicPrefix(x.DisplayName)
                ?? x.DisplayName
                ?? "document";
            var folder = meta?.SuggestedFolder ?? x.Facet;
            var topic = DocumentContextLabel.InferTopic(fileName, meta?.FullTextContent, meta?.AiTags, folder);
            var summary = DocumentContextLabel.BuildSummary(meta?.FullTextContent);
            return new SemanticSearchHit(
                x.SourceId,
                fileName,
                folder,
                BuildSnippet(x.Content, request.Query, PassageSnippetChars),
                x.Score,
                x.ChunkIndex,
                topic,
                summary);
        });

        // Legacy whole-document vectors for sources not yet chunk-indexed.
        var docs = await db.Documents
            .Where(d => d.Embedding != null && !d.IsTransient && d.DeletedAt == null)
            .Select(d => new { d.Id, d.FileName, d.SuggestedFolder, d.FullTextContent, d.AiTags, d.Embedding })
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
                var topic = DocumentContextLabel.InferTopic(d.FileName, d.FullTextContent, d.AiTags, d.SuggestedFolder);
                var summary = DocumentContextLabel.BuildSummary(d.FullTextContent);
                return new SemanticSearchHit(d.Id, d.FileName, d.SuggestedFolder, snippet, score, null, topic, summary);
            })
            .Where(h => h.Score >= minScore);

        var hits = chunkHits.Concat(legacyHits)
            .OrderByDescending(h => h.Score)
            .Take(topK)
            .ToList();

        var considered = chunkSourceIds.Count + legacyDocs.Count;
        TikrActionLog.Completed(logger, "AI.SemanticSearchDocuments",
            $"Hits={hits.Count} Considered={considered} TopScore={(hits.Count > 0 ? hits[0].Score.ToString("F3") : "n/a")}",
            sw.ElapsedMilliseconds);
        return new SemanticSearchResponse(request.Query, considered, hits, EmbeddingAvailable: true);
    }

    public async Task<EmbedKnowledgeEntryResponse> EmbedKnowledgeEntryAsync(Guid entryId, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        TikrActionLog.Started(logger, "AI.EmbedKnowledge", $"EntryId={entryId}");

        var entry = await db.KnowledgeEntries.FindAsync([entryId], cancellationToken)
            ?? throw new KeyNotFoundException($"Knowledge entry {entryId} not found.");

        var ok = await TryIndexKnowledgeChunksAsync(entry, cancellationToken);
        if (!ok)
        {
            TikrActionLog.Failed(logger, "AI.EmbedKnowledge", "Embedding generator unavailable", $"EntryId={entryId}");
            return new EmbedKnowledgeEntryResponse(entryId, false, "Embedding generator unavailable (is Ollama running with nomic-embed-text?)");
        }

        entry.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        TikrActionLog.Completed(logger, "AI.EmbedKnowledge", $"EntryId={entryId} Title={entry.Title}", sw.ElapsedMilliseconds);
        return new EmbedKnowledgeEntryResponse(entryId, true, null);
    }

    public async Task<SemanticSearchKnowledgeResponse> SemanticSearchKnowledgeAsync(SemanticSearchRequest request, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        TikrActionLog.Started(logger, "AI.SemanticSearchKnowledge",
            $"QueryLen={request.Query?.Length ?? 0} TopK={request.TopK}");

        if (string.IsNullOrWhiteSpace(request.Query))
            return new SemanticSearchKnowledgeResponse(request.Query ?? string.Empty, 0, [], EmbeddingAvailable: true);

        var topK = Math.Clamp(request.TopK, 1, 20);
        var minScore = request.MinScore ?? DefaultMinScore;

        var queryVector = await TryGenerateEmbeddingAsync(request.Query, cancellationToken);
        if (queryVector is null)
        {
            TikrActionLog.Failed(logger, "AI.SemanticSearchKnowledge", "Embeddings unavailable (Ollama/nomic-embed-text)");
            return new SemanticSearchKnowledgeResponse(request.Query, 0, [], EmbeddingAvailable: false);
        }

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
        TikrActionLog.Completed(logger, "AI.SemanticSearchKnowledge",
            $"Hits={hits.Count} Considered={considered}",
            sw.ElapsedMilliseconds);
        return new SemanticSearchKnowledgeResponse(request.Query, considered, hits, EmbeddingAvailable: true);
    }

    public async Task<ReindexEmbeddingsResponse> ReindexAllEmbeddingsAsync(
        string? trigger = null,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var triggerLabel = string.IsNullOrWhiteSpace(trigger) ? "manual" : trigger.Trim();
        TikrActionLog.Started(logger, "AI.ReindexEmbeddings", $"Trigger={triggerLabel}");

        var errors = new List<string>();
        // Active library only: skip recycle-bin and one-time filings (not in Assistant RAG).
        var docs = await db.Documents
            .Where(d => d.DeletedAt == null && !d.IsTransient)
            .ToListAsync(cancellationToken);
        var skipped = await db.Documents.CountAsync(
            d => d.DeletedAt != null || d.IsTransient,
            cancellationToken);
        var entries = await db.KnowledgeEntries.ToListAsync(cancellationToken);
        var docsOk = 0;
        var knowledgeOk = 0;

        foreach (var doc in docs)
        {
            try
            {
                var sourceText = BuildEmbeddingText(doc);
                if (IsSparseForEmbedding(sourceText))
                {
                    errors.Add($"Document {doc.FileName}: sparse/missing text (OCR or re-tag may help)");
                    continue;
                }

                if (await TryIndexDocumentChunksAsync(doc, cancellationToken))
                    docsOk++;
                else
                    errors.Add($"Document {doc.FileName}: embedding generator unavailable (is Ollama + nomic-embed-text up?)");
            }
            catch (Exception ex)
            {
                errors.Add($"Document {doc.FileName}: {ex.Message}");
            }
        }

        foreach (var entry in entries)
        {
            try
            {
                if (await TryIndexKnowledgeChunksAsync(entry, cancellationToken))
                    knowledgeOk++;
                else
                    errors.Add($"Knowledge {entry.Title}: embedding generator unavailable");
            }
            catch (Exception ex)
            {
                errors.Add($"Knowledge {entry.Title}: {ex.Message}");
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        TikrActionLog.Completed(logger, "AI.ReindexEmbeddings",
            $"Trigger={triggerLabel} Docs={docsOk}/{docs.Count} Knowledge={knowledgeOk}/{entries.Count} Skipped={skipped} Errors={errors.Count}",
            sw.ElapsedMilliseconds);
        return new ReindexEmbeddingsResponse(
            docs.Count,
            docsOk,
            entries.Count,
            knowledgeOk,
            errors,
            DocumentsSkipped: skipped,
            Trigger: triggerLabel);
    }

    public async Task<CorpusHealthResponse> GetCorpusHealthAsync(CancellationToken cancellationToken = default)
    {
        // Coverage is for active, recurring (non-transient) documents only.
        var documents = await db.Documents
            .Where(d => d.DeletedAt == null)
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

        // Recovery needs: embeddable recurring docs missing chunks (exclude sparse — reindex cannot fix those).
        var embeddableMissing = recurring.Count(d =>
            !IsSparseForEmbedding(d.FullTextContent) && !docChunkSet.Contains(d.Id));

        var knowledgeWithChunks = knowledge.Count(id => knowledgeChunkSet.Contains(id));
        var docPct = recurring.Count == 0 ? 100.0 : Math.Round(100.0 * withChunks / recurring.Count, 1);
        var knowledgePct = knowledge.Count == 0 ? 100.0 : Math.Round(100.0 * knowledgeWithChunks / knowledge.Count, 1);

        var needsAttention = sparse.ToList();
        if (embeddableMissing > 0)
            needsAttention.Insert(0, $"{embeddableMissing} document(s) ready for reindex (Ollama offline earlier?)");

        return new CorpusHealthResponse(
            DocumentsTotal: recurring.Count,
            DocumentsWithChunks: withChunks,
            DocumentsTransient: documents.Count(d => d.IsTransient),
            DocumentsSparseText: sparse.Count,
            KnowledgeTotal: knowledge.Count,
            KnowledgeWithChunks: knowledgeWithChunks,
            DocumentsChunkCoveragePercent: docPct,
            KnowledgeChunkCoveragePercent: knowledgePct,
            NeedsAttention: needsAttention);
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
        var sw = Stopwatch.StartNew();
        TikrActionLog.Started(logger, "AI.AskAdvanced",
            $"PromptLen={request.Prompt?.Length ?? 0} ContextLen={request.Context?.Length ?? 0}");

        var prompt = string.IsNullOrWhiteSpace(request.Context)
            ? request.Prompt ?? string.Empty
            : $"Context:\n{request.Context}\n\nQuestion:\n{request.Prompt ?? string.Empty}";

        bool ollamaAvailable = false;
        try
        {
            ollamaAvailable = await ollamaFactory.IsAvailableAsync(cancellationToken);
        }
        catch { /* best effort */ }

        // PreferCloud: Web already ran AssistantAgentRouter and chose Grok.
        // Otherwise re-score the clerk question (API-only callers).
        var routePrompt = string.IsNullOrWhiteSpace(request.Prompt) ? prompt : request.Prompt;
        var decision = request.PreferCloud && grokService.IsEnabled
            ? new AiRouteDecision(AiRoute.CloudGrok, "Caller PreferCloud", 1.0)
            : AssistantAgentRouter.Decide(
                routePrompt,
                ollamaAvailable: ollamaAvailable,
                grokEnabled: grokService.IsEnabled);

        if (decision.Route == AiRoute.CloudGrok && grokService.IsEnabled)
        {
            var grokAnswer = await grokService.CompleteAsync(prompt, cancellationToken: cancellationToken);
            if (!string.IsNullOrWhiteSpace(grokAnswer))
            {
                TikrActionLog.Completed(logger, "AI.AskAdvanced",
                    $"UsedGrok=true Route={decision.Reason} AnswerLen={grokAnswer.Length} OllamaAvailable={ollamaAvailable}",
                    sw.ElapsedMilliseconds);
                return new AskAdvancedResponse(grokAnswer, UsedGrok: true);
            }
        }

        var localAnswer = await GetLocalCompletionAsync(prompt, cancellationToken);
        if (!string.IsNullOrWhiteSpace(localAnswer))
        {
            TikrActionLog.Completed(logger, "AI.AskAdvanced",
                $"UsedGrok=false Route={decision.Reason} AnswerLen={localAnswer.Length} OllamaAvailable={ollamaAvailable}",
                sw.ElapsedMilliseconds);
            return new AskAdvancedResponse(localAnswer, UsedGrok: false);
        }

        if (grokService.IsEnabled)
        {
            var escalate = AssistantAgentRouter.EscalateAfterLocalFailure(grokEnabled: true);
            var grokAnswer = await grokService.CompleteAsync(prompt, cancellationToken: cancellationToken);
            if (!string.IsNullOrWhiteSpace(grokAnswer))
            {
                TikrActionLog.Completed(logger, "AI.AskAdvanced",
                    $"UsedGrok=true ({escalate.Reason}) AnswerLen={grokAnswer.Length}",
                    sw.ElapsedMilliseconds);
                return new AskAdvancedResponse(grokAnswer, UsedGrok: true);
            }
        }

        TikrActionLog.Failed(logger, "AI.AskAdvanced", "No answer from Ollama or Grok");
        return new AskAdvancedResponse("Unable to get a response. Check Ollama connectivity (or enable USE_GROK with a valid GROK_API_KEY).", UsedGrok: false);
    }

    public async Task<AiStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var ollamaAvailable = await ollamaFactory.IsAvailableAsync(cancellationToken);
        var keyConfigured = featureSettings?.Current.GrokApiKeyConfigured ?? grokService.IsEnabled;
        return new AiStatusResponse(
            ollamaAvailable,
            ollamaFactory.ChatModel,
            grokService.IsEnabled,
            ollamaFactory.OllamaHost,
            keyConfigured);
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

        // Topic-prefixed DisplayName improves keyword ranking and agent labels for generic scan names.
        var topic = DocumentContextLabel.InferTopic(
            document.FileName, document.FullTextContent, document.AiTags, document.SuggestedFolder);
        var displayName = DocumentContextLabel.BuildSourceLabel(document.FileName, topic);

        if (existing.Count > 0 && existing.All(c => c.ContentHash == sourceHash))
        {
            // Same body: keep vectors, but refresh labels/facets when tagging improves.
            var facet = document.SuggestedFolder;
            var labelsStale = existing.Any(c =>
                !string.Equals(c.DisplayName, displayName, StringComparison.Ordinal) ||
                !string.Equals(c.Facet, facet, StringComparison.Ordinal));
            if (labelsStale)
            {
                foreach (var chunk in existing)
                {
                    chunk.DisplayName = displayName;
                    chunk.Facet = facet;
                    chunk.UpdatedAt = DateTime.UtcNow;
                }
            }
            return true;
        }

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
                DisplayName = displayName,
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

    /// <summary>
    /// When chunk DisplayName was stored as "[Topic] file.pdf", recover the bare file name.
    /// </summary>
    internal static string? StripTopicPrefix(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return null;
        var s = displayName.Trim();
        if (s.Length < 4 || s[0] != '[')
            return s;
        var close = s.IndexOf(']');
        if (close <= 0 || close >= s.Length - 1)
            return s;
        var remainder = s[(close + 1)..].Trim();
        return string.IsNullOrWhiteSpace(remainder) ? s : remainder;
    }
}
