namespace TIKR.Shared.DTOs;

public record TagDocumentRequest(Guid DocumentId);

public record TagDocumentResponse(
    Guid DocumentId,
    string[] Tags,
    string? SuggestedFolder,
    string? ProposedFolder = null);

public record FolderProposalGroupDto(string Name, int Count);

public record FolderProposalDecisionRequest(string Name);

public record FolderProposalDecisionResponse(string Name, int Updated);

public record DashboardPriority(
    string Title,
    string Reason,
    DateOnly? DueDate,
    string Priority,
    string? SubmitTo = null,
    string? ContactName = null,
    string? ContactEmail = null,
    string? ContactPhone = null);

/// <summary>One prior clerk or assistant turn sent with an advanced ask.</summary>
public record AssistantTurn(string Role, string Content);

/// <param name="PreferCloud">When true (Assistant auto-route), try Grok first if enabled.</param>
/// <param name="History">Prior turns, oldest first. Same window the local model already sees.</param>
public record AskAdvancedRequest(
    string Prompt,
    string? Context,
    bool PreferCloud = false,
    IReadOnlyList<AssistantTurn>? History = null);

/// <param name="GrokAttemptFailed">True when a cloud call was attempted and returned nothing.</param>
public record AskAdvancedResponse(string Answer, bool UsedGrok, bool GrokAttemptFailed = false);

/// <param name="StatusMessage">Clerk-facing note when models are missing, circuit is open, or Ollama is down.</param>
/// <param name="ChatModelPresent">True when the configured chat model appears in Ollama /api/tags.</param>
/// <param name="EmbeddingModelPresent">True when nomic-embed-text appears in Ollama /api/tags.</param>
/// <param name="CircuitOpen">True when the Ollama circuit breaker is open (fail-soft pause).</param>
/// <param name="LoadedModelCount">Models currently loaded in Ollama RAM (/api/ps).</param>
public record AiStatusResponse(
    bool OllamaAvailable,
    string OllamaModel,
    bool GrokEnabled,
    string? OllamaHost = null,
    bool GrokApiKeyConfigured = false,
    string? StatusMessage = null,
    bool ChatModelPresent = true,
    bool EmbeddingModelPresent = true,
    bool CircuitOpen = false,
    int LoadedModelCount = 0);

public record SemanticSearchRequest(
    string Query,
    int TopK = 3,
    double? MinScore = null,
    string? Folder = null,
    string? Category = null);

/// <param name="Topic">Content-derived topic (e.g. Retirement Package Form DD-2656) for agent labels.</param>
/// <param name="Summary">Short document orientation blurb shown before the matched excerpt.</param>
public record SemanticSearchHit(
    Guid DocumentId,
    string FileName,
    string? SuggestedFolder,
    string? Snippet,
    double Score,
    int? ChunkIndex = null,
    string? Topic = null,
    string? Summary = null);

public record SemanticSearchResponse(
    string Query,
    int Considered,
    IReadOnlyList<SemanticSearchHit> Hits,
    bool EmbeddingAvailable = true);

public record EmbedDocumentResponse(Guid DocumentId, bool Embedded, string? Reason);

public record SemanticSearchKnowledgeHit(
    Guid EntryId,
    string Title,
    string Category,
    string? Snippet,
    double Score,
    int? ChunkIndex = null);

public record SemanticSearchKnowledgeResponse(
    string Query,
    int Considered,
    IReadOnlyList<SemanticSearchKnowledgeHit> Hits,
    bool EmbeddingAvailable = true);

public record EmbedKnowledgeEntryResponse(Guid EntryId, bool Embedded, string? Reason);

public record ReindexEmbeddingsResponse(
    int DocumentsAttempted,
    int DocumentsEmbedded,
    int KnowledgeAttempted,
    int KnowledgeEmbedded,
    IReadOnlyList<string> Errors,
    int DocumentsSkipped = 0,
    string? Trigger = null);

/// <summary>Operator snapshot for automatic embedding recovery (Settings + diagnostics).</summary>
public record EmbeddingRecoveryStatusDto(
    bool OllamaAvailable,
    bool RecoveryNeeded,
    DateTime? LastOllamaHealthyUtc,
    DateTime? LastAutoReindexUtc,
    string? LastTrigger,
    string? LastResultSummary,
    string? LastError,
    double DocumentsChunkCoveragePercent,
    double KnowledgeChunkCoveragePercent);
