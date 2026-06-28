using TIKR.Shared.DTOs;
using TIKR.Shared.Interfaces;

namespace TIKR.Api.Tests.Fixtures;

/// <summary>
/// Deterministic AI stub for API integration tests — no Ollama/Grok network calls.
/// </summary>
public sealed class StubHybridAiService : IHybridAiService
{
    public Task<TagDocumentResponse> TagDocumentAsync(Guid documentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new TagDocumentResponse(documentId, ["stub-tag"], "StubFolder"));

    public Task<IReadOnlyList<DashboardPriority>> GetDashboardPrioritiesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DashboardPriority>>([
            new DashboardPriority("Stub priority", "From stub AI", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)), "High")
        ]);

    public Task<AskAdvancedResponse> AskAdvancedAsync(AskAdvancedRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AskAdvancedResponse($"Stub answer to: {request.Prompt}", UsedGrok: true));

    public Task<AiStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new AiStatusResponse(OllamaAvailable: true, OllamaModel: "stub-model", GrokEnabled: true));

    public Task<SemanticSearchResponse> SemanticSearchDocumentsAsync(SemanticSearchRequest request, CancellationToken cancellationToken = default)
    {
        var hits = string.IsNullOrWhiteSpace(request.Query)
            ? Array.Empty<SemanticSearchHit>()
            : new[] { new SemanticSearchHit(Guid.NewGuid(), "stub-doc.pdf", "Finance", "stub snippet", 0.95) };
        return Task.FromResult(new SemanticSearchResponse(request.Query, 1, hits));
    }

    public Task<EmbedDocumentResponse> EmbedDocumentAsync(Guid documentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new EmbedDocumentResponse(documentId, Embedded: true, Reason: null));

    public Task<SemanticSearchKnowledgeResponse> SemanticSearchKnowledgeAsync(SemanticSearchRequest request, CancellationToken cancellationToken = default)
    {
        var hits = string.IsNullOrWhiteSpace(request.Query)
            ? Array.Empty<SemanticSearchKnowledgeHit>()
            : new[] { new SemanticSearchKnowledgeHit(Guid.NewGuid(), "Stub entry", "HowTo", "stub snippet", 0.88) };
        return Task.FromResult(new SemanticSearchKnowledgeResponse(request.Query, 1, hits));
    }

    public Task<EmbedKnowledgeEntryResponse> EmbedKnowledgeEntryAsync(Guid entryId, CancellationToken cancellationToken = default) =>
        Task.FromResult(new EmbedKnowledgeEntryResponse(entryId, Embedded: true, Reason: null));
}
