using TIKR.Shared.DTOs;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// Process-wide snapshot of automatic embedding recovery (singleton).
/// Updated by <see cref="EmbeddingRecoveryHostedService"/> and manual reindex hooks.
/// </summary>
public sealed class EmbeddingRecoveryState
{
    private readonly object _gate = new();

    public bool LastOllamaAvailable { get; private set; }
    public DateTime? LastOllamaHealthyUtc { get; private set; }
    public DateTime? LastOllamaUnhealthyUtc { get; private set; }
    public DateTime? LastAutoReindexUtc { get; private set; }
    public string? LastTrigger { get; private set; }
    public string? LastResultSummary { get; private set; }
    public string? LastError { get; private set; }
    public double LastDocumentsCoveragePercent { get; private set; } = 100;
    public double LastKnowledgeCoveragePercent { get; private set; } = 100;
    public bool RecoveryNeeded { get; private set; }

    public void NoteOllama(bool available, DateTime utcNow)
    {
        lock (_gate)
        {
            LastOllamaAvailable = available;
            if (available)
                LastOllamaHealthyUtc = utcNow;
            else
                LastOllamaUnhealthyUtc = utcNow;
        }
    }

    public void NoteCorpus(CorpusHealthResponse health)
    {
        lock (_gate)
        {
            LastDocumentsCoveragePercent = health.DocumentsChunkCoveragePercent;
            LastKnowledgeCoveragePercent = health.KnowledgeChunkCoveragePercent;
            RecoveryNeeded = NeedsRecovery(health);
        }
    }

    public void NoteReindexResult(string trigger, ReindexEmbeddingsResponse result, DateTime utcNow)
    {
        lock (_gate)
        {
            LastTrigger = trigger;
            LastAutoReindexUtc = utcNow;
            LastResultSummary =
                $"docs {result.DocumentsEmbedded}/{result.DocumentsAttempted}, " +
                $"vault {result.KnowledgeEmbedded}/{result.KnowledgeAttempted}, " +
                $"skipped {result.DocumentsSkipped}, errors {result.Errors.Count}";
            LastError = result.Errors.Count > 0
                ? string.Join("; ", result.Errors.Take(3))
                : null;
        }
    }

    public void NoteError(string error)
    {
        lock (_gate)
        {
            LastError = error;
        }
    }

    public EmbeddingRecoveryStatusDto Snapshot()
    {
        lock (_gate)
        {
            return new EmbeddingRecoveryStatusDto(
                OllamaAvailable: LastOllamaAvailable,
                RecoveryNeeded: RecoveryNeeded,
                LastOllamaHealthyUtc: LastOllamaHealthyUtc,
                LastAutoReindexUtc: LastAutoReindexUtc,
                LastTrigger: LastTrigger,
                LastResultSummary: LastResultSummary,
                LastError: LastError,
                DocumentsChunkCoveragePercent: LastDocumentsCoveragePercent,
                KnowledgeChunkCoveragePercent: LastKnowledgeCoveragePercent);
        }
    }

    /// <summary>
    /// True when embeddable corpus is incomplete (coverage &lt; 100% with work remaining).
    /// Sparse-only gaps still show in NeedsAttention but do not force endless reindex.
    /// </summary>
    public static bool NeedsRecovery(CorpusHealthResponse health)
    {
        if (health.DocumentsTotal > 0 &&
            health.DocumentsWithChunks < health.DocumentsTotal &&
            health.DocumentsWithChunks + health.DocumentsSparseText < health.DocumentsTotal)
            return true;

        if (health.KnowledgeTotal > 0 && health.KnowledgeWithChunks < health.KnowledgeTotal)
            return true;

        return false;
    }
}
