namespace TIKR.Shared.Entities;

/// <summary>Playbook / checklist step under a Requirement (election or any complex due-out).</summary>
public class RequirementChecklistItem
{
    public Guid Id { get; set; }
    public Guid RequirementId { get; set; }
    public Requirement Requirement { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsRequired { get; set; } = true;
    public bool IsCompleted { get; set; }

    /// <summary>Days before the parent Requirement.DueDate (ignored when <see cref="DueDate"/> is set).</summary>
    public int? DueOffsetDays { get; set; }

    /// <summary>Absolute due date override for this step.</summary>
    public DateOnly? DueDate { get; set; }

    public int SortOrder { get; set; }

    public Guid? LinkedDocumentId { get; set; }
    public Document? LinkedDocument { get; set; }

    /// <summary>Clerk hint for expected filing / template name (e.g. "canvass-packet.pdf").</summary>
    public string? DocumentTemplateHint { get; set; }

    public string? SubmitTo { get; set; }
    public Guid? ContactId { get; set; }
    public Contact? Contact { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
