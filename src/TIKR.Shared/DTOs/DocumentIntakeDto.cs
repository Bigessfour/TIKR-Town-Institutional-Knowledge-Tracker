namespace TIKR.Shared.DTOs;

public record DocumentIntakeDueDateDto(DateOnly Date, string? Label = null, string? SourceSnippet = null);

public record DocumentIntakeContactDto(
    string Name,
    string? Organization = null,
    string? Email = null,
    string? Phone = null,
    string? Role = null,
    string? Notes = null);

public record DocumentIntakeActionItemDto(
    string Title,
    DateOnly? DueDate = null,
    string? Notes = null);

/// <summary>Serializable payload stored on <c>DocumentIntakeBrief.JsonPayload</c>.</summary>
public record DocumentIntakeBriefPayload(
    IReadOnlyList<DocumentIntakeDueDateDto> DueDates,
    IReadOnlyList<DocumentIntakeContactDto> Contacts,
    IReadOnlyList<DocumentIntakeActionItemDto> ActionItems);

public record DocumentIntakeBriefDto(
    Guid Id,
    Guid DocumentId,
    string FileName,
    string Status,
    IReadOnlyList<DocumentIntakeDueDateDto> DueDates,
    IReadOnlyList<DocumentIntakeContactDto> Contacts,
    IReadOnlyList<DocumentIntakeActionItemDto> ActionItems,
    bool ProcessedLocally,
    bool UsedGrok,
    DateTime CreatedAtUtc);

public record AcceptDocumentIntakeBriefRequest(
    IReadOnlyList<int>? ContactIndexes = null,
    IReadOnlyList<int>? ActionItemIndexes = null,
    IReadOnlyList<int>? DueDateIndexes = null);

public record AcceptDocumentIntakeBriefResponse(
    Guid BriefId,
    int ContactsUpserted,
    int RequirementsCreated,
    IReadOnlyList<Guid> RequirementIds);
