using TIKR.Shared.Enums;

namespace TIKR.Shared.Entities;

public class KnowledgeEntry
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public KnowledgeCategory Category { get; set; } = KnowledgeCategory.HowTo;
    public int SortOrder { get; set; }

    // Phase 9: byte-packed float[] embedding from nomic-embed-text.
    // Mirrors Document.Embedding so the Assistant can semantically retrieve
    // institutional procedural knowledge ("hit by a bus" scenario).
    public byte[]? Embedding { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
