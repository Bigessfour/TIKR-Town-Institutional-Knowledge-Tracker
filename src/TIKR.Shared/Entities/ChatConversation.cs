namespace TIKR.Shared.Entities;

/// <summary>Per-user assistant thread. Isolation is enforced by <see cref="UserId"/> on every query.</summary>
public class ChatConversation
{
    public Guid Id { get; set; }

    /// <summary>Stable clerk key (email preferred, else Identity user id).</summary>
    public string UserId { get; set; } = string.Empty;

    public string Title { get; set; } = "New chat";
    public string? Summary { get; set; }
    public bool IsArchived { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<ChatMessageRecord> Messages { get; set; } = [];
}
