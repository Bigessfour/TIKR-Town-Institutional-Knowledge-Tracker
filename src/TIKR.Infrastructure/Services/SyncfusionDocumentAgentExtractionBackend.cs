using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

public class SyncfusionDocumentAgentExtractionBackend(SyncfusionDocumentAgentExtractor extractor) : IDocumentAgentExtractionBackend
{
    public Task<AgentExtractionResult> ExtractAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default) =>
        extractor.ExtractAsync(content, fileName, cancellationToken);
}
