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
        IReadOnlyList<RequirementLinkedDocumentDto>? linkedDocuments = null) =>
        new(
            id ?? Guid.NewGuid(),
            title,
            description,
            dueDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
            recurrence,
            category,
            isSystemSeeded,
            isCompleted,
            linkedDocuments ?? []);
}
