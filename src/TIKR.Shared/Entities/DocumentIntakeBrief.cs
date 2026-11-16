using TIKR.Shared.Enums;

namespace TIKR.Shared.Entities;

/// <summary>Clerk-confirm intake brief for a document (due dates, contacts, action items).</summary>
public class DocumentIntakeBrief
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public DocumentIntakeBriefStatus Status { get; set; } = DocumentIntakeBriefStatus.Pending;

    /// <summary>Serialized <see cref="TIKR.Shared.DTOs.DocumentIntakeBriefPayload"/> JSON.</summary>
    public string JsonPayload { get; set; } = "{}";

    public bool ProcessedLocally { get; set; } = true;
    public bool UsedGrok { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
