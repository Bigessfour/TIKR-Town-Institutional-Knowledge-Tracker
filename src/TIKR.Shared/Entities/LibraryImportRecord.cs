namespace TIKR.Shared.Entities;

/// <summary>
/// Tracks NAS library files already copied into TIKR Documents so rescans are idempotent.
/// Source files on the NAS are never moved or deleted.
/// </summary>
public class LibraryImportRecord
{
    public Guid Id { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public string ContentFingerprint { get; set; } = string.Empty;
    public Guid DocumentId { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
}
