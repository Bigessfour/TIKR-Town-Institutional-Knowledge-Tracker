using TIKR.Shared.Enums;

namespace TIKR.Shared.DTOs;

public record RequirementLinkedDocumentDto(Guid DocumentId, string FileName, string? Summary);

public record RequirementDto(
    Guid Id,
    string Title,
    string? Description,
    DateOnly DueDate,
    RecurrenceType Recurrence,
    RequirementCategory Category,
    bool IsSystemSeeded,
    bool IsCompleted,
    IReadOnlyList<RequirementLinkedDocumentDto> LinkedDocuments);

public record LinkRequirementDocumentRequest(Guid DocumentId);

public record CreateRequirementRequest(
    string Title,
    string? Description,
    DateOnly DueDate,
    RecurrenceType Recurrence,
    RequirementCategory Category);

public record UpdateRequirementRequest(
    string Title,
    string? Description,
    DateOnly DueDate,
    RecurrenceType Recurrence,
    RequirementCategory Category,
    bool IsCompleted);
