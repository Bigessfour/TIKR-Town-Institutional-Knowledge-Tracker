using TIKR.Shared.DTOs;

namespace TIKR.Shared.Interfaces;

public interface IHybridAiService
{
    /// <param name="libraryRelativePath">
    /// Optional NAS-relative path from library scan (e.g. COUNCIL MEETINGS/2024/agenda.pdf).
    /// Seeds folder classification and appears in the tag prompt.
    /// </param>
    Task<TagDocumentResponse> TagDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default,
        string? libraryRelativePath = null);
    Task<IReadOnlyList<DashboardPriority>> GetDashboardPrioritiesAsync(CancellationToken cancellationToken = default);
    Task<AskAdvancedResponse> AskAdvancedAsync(AskAdvancedRequest request, CancellationToken cancellationToken = default);
    Task<AiStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<SemanticSearchResponse> SemanticSearchDocumentsAsync(SemanticSearchRequest request, CancellationToken cancellationToken = default);
    Task<EmbedDocumentResponse> EmbedDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<SemanticSearchKnowledgeResponse> SemanticSearchKnowledgeAsync(SemanticSearchRequest request, CancellationToken cancellationToken = default);
    Task<EmbedKnowledgeEntryResponse> EmbedKnowledgeEntryAsync(Guid entryId, CancellationToken cancellationToken = default);
    /// <param name="trigger">Label for logs/Settings (manual, auto-recovery, etc.).</param>
    Task<ReindexEmbeddingsResponse> ReindexAllEmbeddingsAsync(
        string? trigger = null,
        CancellationToken cancellationToken = default);

    Task<CorpusHealthResponse> GetCorpusHealthAsync(CancellationToken cancellationToken = default);
}
