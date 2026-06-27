using TIKR.Shared.Enums;

namespace TIKR.Shared.DTOs;

public record RequirementDto(
    Guid Id,
    string Title,
    string? Description,
    DateOnly DueDate,
    RecurrenceType Recurrence,
    RequirementCategory Category,
    bool IsSystemSeeded,
    bool IsCompleted);

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
