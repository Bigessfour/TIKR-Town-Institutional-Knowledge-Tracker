namespace TIKR.Shared.DTOs;

public record DocumentDto(
    Guid Id,
    string FileName,
    string? ContentType,
    long FileSizeBytes,
    string? AiTags,
    string? SuggestedFolder,
    DateTime UploadedAt,
    string? FullTextContent = null);

public record DocumentSearchResult(
    Guid Id,
    string FileName,
    string? AiTags,
    string? SuggestedFolder,
    DateTime UploadedAt,
    string? Snippet);
