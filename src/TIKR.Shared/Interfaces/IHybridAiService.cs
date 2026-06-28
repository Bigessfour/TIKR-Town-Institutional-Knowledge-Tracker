using TIKR.Shared.DTOs;

namespace TIKR.Shared.Interfaces;

public interface IHybridAiService
{
    Task<TagDocumentResponse> TagDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DashboardPriority>> GetDashboardPrioritiesAsync(CancellationToken cancellationToken = default);
    Task<AskAdvancedResponse> AskAdvancedAsync(AskAdvancedRequest request, CancellationToken cancellationToken = default);
    Task<AiStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<SemanticSearchResponse> SemanticSearchDocumentsAsync(SemanticSearchRequest request, CancellationToken cancellationToken = default);
    Task<EmbedDocumentResponse> EmbedDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
}
