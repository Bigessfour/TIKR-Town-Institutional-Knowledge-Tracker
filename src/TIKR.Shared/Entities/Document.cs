namespace TIKR.Shared.Entities;

public class Document
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public string? AiTags { get; set; }
    public string? SuggestedFolder { get; set; }
    public string? FullTextContent { get; set; }

    // Phase 9: byte-packed float[] vector from nomic-embed-text (typically 768 floats = 3072 bytes).
    // Null until embedding generation runs successfully against Ollama.
    public byte[]? Embedding { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
