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
    bool IsTransient = false);

public record DocumentSearchResult(
    Guid Id,
    string FileName,
    string? AiTags,
    string? SuggestedFolder,
    DateTime UploadedAt,
    string? Snippet);

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
