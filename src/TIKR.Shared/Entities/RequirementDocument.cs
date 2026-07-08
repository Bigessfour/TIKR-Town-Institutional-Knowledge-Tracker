namespace TIKR.Shared.Entities;

public class RequirementDocument
{
    public Guid RequirementId { get; set; }
    public Requirement Requirement { get; set; } = null!;
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
}