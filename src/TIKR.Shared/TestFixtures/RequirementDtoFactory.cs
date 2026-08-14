using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;

namespace TIKR.Shared.TestFixtures;

public static class RequirementDtoFactory
{
    public static RequirementDto Create(
        Guid? id = null,
        string title = "Requirement",
        string? description = null,
        DateOnly? dueDate = null,
        RecurrenceType recurrence = RecurrenceType.Annual,
        RequirementCategory category = RequirementCategory.Custom,
        bool isSystemSeeded = false,
        bool isCompleted = false,
        IReadOnlyList<RequirementLinkedDocumentDto>? linkedDocuments = null,
        string? submitTo = null,
        string? contactName = null,
        string? contactEmail = null,
        string? contactPhone = null,
        int checklistCompleted = 0,
        int checklistTotal = 0) =>
        new(
            id ?? Guid.NewGuid(),
            title,
            description,
            dueDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            recurrence,
            category,
            isSystemSeeded,
            isCompleted,
            linkedDocuments ?? [],
            submitTo,
            contactName,
            contactEmail,
            contactPhone,
            checklistCompleted,
            checklistTotal);
}
