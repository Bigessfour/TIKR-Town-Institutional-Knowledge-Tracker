namespace TIKR.Shared.Entities;

/// <summary>
/// Prior content snapshot kept when a clerk saves edits (PDF annotations, Word, Excel).
/// Points at a previous StoragePath under file storage; limited retention (see DocumentService).
/// </summary>
public class DocumentVersion
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByUserId { get; set; }
}
