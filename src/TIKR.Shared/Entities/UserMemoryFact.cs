namespace TIKR.Shared.Entities;

/// <summary>
/// Durable per-user fact (birthday, preferred name, etc.) injected into the assistant prompt —
/// not the full chat transcript.
/// </summary>
public class UserMemoryFact
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;

    /// <summary>Stable key, e.g. birthday, preferred_name, note.</summary>
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
    public Guid? SourceMessageId { get; set; }
    public bool Confirmed { get; set; } = true;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
