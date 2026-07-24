namespace TIKR.Shared.DTOs;

public record TagDocumentRequest(Guid DocumentId);

public record TagDocumentResponse(Guid DocumentId, string[] Tags, string? SuggestedFolder);

public record DashboardPriority(
    string Title,
    string Reason,
    DateOnly? DueDate,
    string Priority);

public record AskAdvancedRequest(string Prompt, string? Context);

public record AskAdvancedResponse(string Answer, bool UsedGrok);

public record AiStatusResponse(
    bool OllamaAvailable,
    string OllamaModel,
    bool GrokEnabled);

public record SemanticSearchRequest(
    string Query,
    int TopK = 3,
    double? MinScore = null,
    string? Folder = null,
    string? Category = null);

public record SemanticSearchHit(
    Guid DocumentId,
    string FileName,
    string? SuggestedFolder,
    string? Snippet,
    double Score,
    int? ChunkIndex = null);

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
    IReadOnlyList<string> Errors);
