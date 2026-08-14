namespace TIKR.Shared.Entities;

public class RequirementContact
{
    public Guid RequirementId { get; set; }
    public Requirement Requirement { get; set; } = null!;
    public Guid ContactId { get; set; }
    public Contact Contact { get; set; } = null!;
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
    public bool IsPrimary { get; set; }
}
