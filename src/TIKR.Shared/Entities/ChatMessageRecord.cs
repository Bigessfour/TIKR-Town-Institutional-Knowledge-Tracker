namespace TIKR.Shared.Entities;

/// <summary>One stored assistant turn message (user or assistant). Named to avoid clash with MEAI ChatMessage.</summary>
public class ChatMessageRecord
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }

    /// <summary>Denormalized owner for isolation filters without joins.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>"user" or "assistant".</summary>
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public ChatConversation? Conversation { get; set; }
}
