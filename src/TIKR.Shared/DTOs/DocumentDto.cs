namespace TIKR.Shared.DTOs;

public record DocumentDto(
    Guid Id,
    string FileName,
    string? ContentType,
    long FileSizeBytes,
    string? AiTags,
    string? SuggestedFolder,
    DateTime UploadedAt,
    string? FullTextContent = null,
    bool IsTransient = false,
    DateTime? DeletedAt = null,
    int LinkedRequirementCount = 0,
    int VersionCount = 0);

public record DocumentVersionDto(
    Guid Id,
    Guid DocumentId,
    int VersionNumber,
    string FileName,
    long FileSizeBytes,
    string? Note,
    DateTime CreatedAt);

public record DocumentRequirementLinkDto(
    Guid RequirementId,
    string Title,
    DateOnly? DueDate);

public record DocumentSearchResult(
    Guid Id,
    string FileName,
    string? AiTags,
    string? SuggestedFolder,
    DateTime UploadedAt,
    string? Snippet);

/// <summary>PATCH body for rename / folder move from File Manager Browse mode.</summary>
public record UpdateDocumentMetadataRequest(
    string? FileName = null,
    string? SuggestedFolder = null,
    bool ClearSuggestedFolder = false);

/// <summary>Corpus completeness snapshot for high-accuracy library compilation.</summary>
public record CorpusHealthResponse(
    int DocumentsTotal,
    int DocumentsWithChunks,
    int DocumentsTransient,
    int DocumentsSparseText,
    int KnowledgeTotal,
    int KnowledgeWithChunks,
    double DocumentsChunkCoveragePercent,
    double KnowledgeChunkCoveragePercent,
    IReadOnlyList<string> NeedsAttention);
