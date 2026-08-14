namespace TIKR.Shared.DTOs;

/// <summary>Result of scanning a local forward-to-folder inbox into Documents (+ optional structured extract).</summary>
public record EmailIngestionResult(
    int Ingested,
    int Skipped,
    IReadOnlyList<string> Errors,
    int ContactsCreated = 0,
    int ContactsUpdated = 0,
    int KnowledgeCreated = 0,
    IReadOnlyList<EmailExtractionNoticeDto>? Notices = null);

public record EmailExtractionNoticeDto(
    DateTime AtUtc,
    string FileName,
    Guid? DocumentId,
    IReadOnlyList<string> ContactNames,
    int ContactsCreated,
    int ContactsUpdated,
    Guid? KnowledgeEntryId,
    EmailRequirementSuggestionDto? RequirementSuggestion,
    string? Message,
    bool ParseSucceeded);

public record EmailRequirementSuggestionDto(
    string Title,
    DateOnly? DueDate,
    string? SubmitTo,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string SourceFileName,
    Guid? DocumentId);
