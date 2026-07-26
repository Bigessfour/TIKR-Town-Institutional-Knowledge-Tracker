using System.ComponentModel.DataAnnotations;
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
    IReadOnlyList<RequirementLinkedDocumentDto> LinkedDocuments,
    string? SubmitTo = null,
    string? ContactName = null,
    string? ContactEmail = null,
    string? ContactPhone = null);

public record LinkRequirementDocumentRequest(Guid DocumentId);

public record CreateRequirementRequest(
    [Required, MaxLength(500)] string Title,
    string? Description,
    DateOnly DueDate,
    RecurrenceType Recurrence,
    RequirementCategory Category,
    [MaxLength(300)] string? SubmitTo = null,
    [MaxLength(200)] string? ContactName = null,
    [MaxLength(200)] string? ContactEmail = null,
    [MaxLength(50)] string? ContactPhone = null);

public record UpdateRequirementRequest(
    [Required, MaxLength(500)] string Title,
    string? Description,
    DateOnly DueDate,
    RecurrenceType Recurrence,
    RequirementCategory Category,
    bool IsCompleted,
    [MaxLength(300)] string? SubmitTo = null,
    [MaxLength(200)] string? ContactName = null,
    [MaxLength(200)] string? ContactEmail = null,
    [MaxLength(50)] string? ContactPhone = null);
