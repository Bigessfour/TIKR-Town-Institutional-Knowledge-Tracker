using TIKR.Shared.Enums;

namespace TIKR.Shared.Entities;

/// <summary>
/// Indexed passage for clerk RAG. One document/vault entry yields many chunks so long
/// text remains searchable beyond a single truncated embedding.
/// </summary>
public class EmbeddingChunk
{
    public Guid Id { get; set; }
    public EmbeddingSourceType SourceType { get; set; }
    public Guid SourceId { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public byte[] Embedding { get; set; } = [];
    public string ContentHash { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Facet { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
