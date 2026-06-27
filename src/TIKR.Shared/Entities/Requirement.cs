using TIKR.Shared.Enums;

namespace TIKR.Shared.Entities;

public class Requirement
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly DueDate { get; set; }
    public RecurrenceType Recurrence { get; set; } = RecurrenceType.Annual;
    public RequirementCategory Category { get; set; } = RequirementCategory.Custom;
    public bool IsSystemSeeded { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}
