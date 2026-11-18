namespace TIKR.Shared.DTOs;

/// <param name="AiStatusMessage">Clerk-facing Ollama / model / circuit note from AI status.</param>
/// <param name="LastLibraryScanUtc">Last library scan completion (hosted or manual).</param>
/// <param name="LastLibraryScanSummary">Short last-scan counts.</param>
/// <param name="LastEmailIngestUtc">Last email inbox ingest completion.</param>
/// <param name="LastEmailIngestSummary">Short last-ingest counts.</param>
/// <param name="LastEmbeddingRecoveryUtc">Last automatic embedding recovery / reindex.</param>
/// <param name="LastEmbeddingRecoverySummary">Last recovery result summary.</param>
/// <param name="EmbeddingRecoveryNeeded">True when corpus coverage still needs attention.</param>
public record LocalStorageStatusDto(
    string TownName,
    string StorageLabel,
    DateTime? DataLastModifiedUtc,
    bool OllamaAvailable,
    string? AiStatusMessage = null,
    DateTime? LastLibraryScanUtc = null,
    string? LastLibraryScanSummary = null,
    DateTime? LastEmailIngestUtc = null,
    string? LastEmailIngestSummary = null,
    DateTime? LastEmbeddingRecoveryUtc = null,
    string? LastEmbeddingRecoverySummary = null,
    bool EmbeddingRecoveryNeeded = false);
