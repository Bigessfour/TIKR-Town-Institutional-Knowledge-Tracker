using System.ComponentModel.DataAnnotations;

namespace TIKR.Shared.DTOs;

public record RequirementChecklistItemDto(
    Guid Id,
    Guid RequirementId,
    string Title,
    string? Description,
    bool IsRequired,
    bool IsCompleted,
    int? DueOffsetDays,
    DateOnly? DueDate,
    DateOnly? EffectiveDueDate,
    int SortOrder,
    Guid? LinkedDocumentId,
    string? DocumentTemplateHint,
    string? SubmitTo,
    Guid? ContactId);

public record CreateRequirementChecklistItemRequest(
    [Required, MaxLength(300)] string Title,
    [MaxLength(2000)] string? Description = null,
    bool IsRequired = true,
    int? DueOffsetDays = null,
    DateOnly? DueDate = null,
    int? SortOrder = null,
    Guid? LinkedDocumentId = null,
    [MaxLength(200)] string? DocumentTemplateHint = null,
    [MaxLength(300)] string? SubmitTo = null,
    Guid? ContactId = null);

public record UpdateRequirementChecklistItemRequest(
    [Required, MaxLength(300)] string Title,
    [MaxLength(2000)] string? Description = null,
    bool IsRequired = true,
    bool IsCompleted = false,
    int? DueOffsetDays = null,
    DateOnly? DueDate = null,
    int SortOrder = 0,
    Guid? LinkedDocumentId = null,
    [MaxLength(200)] string? DocumentTemplateHint = null,
    [MaxLength(300)] string? SubmitTo = null,
    Guid? ContactId = null);

public record ReorderRequirementChecklistRequest(IReadOnlyList<Guid> OrderedIds);

public record CompleteRequirementChecklistItemRequest(bool IsCompleted = true);
