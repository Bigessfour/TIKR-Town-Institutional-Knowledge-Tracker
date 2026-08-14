using TIKR.Shared.Enums;

namespace TIKR.Shared.Entities;

public class Contact
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string? Organization { get; set; }
    public string? Address { get; set; }
    public string? Office { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
    public ContactCategory Categories { get; set; } = ContactCategory.Custom;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }

    /// <summary>Soft-delete timestamp. Null = active; set when clerk deletes (restorable).</summary>
    public DateTime? DeletedAt { get; set; }
}
