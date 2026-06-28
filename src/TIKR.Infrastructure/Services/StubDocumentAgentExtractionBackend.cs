using TIKR.Shared.Enums;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// MVP extraction: real plain-text via DocumentTextExtractionService; PDF/DOCX heuristics until Syncfusion tools land.
/// </summary>
public class StubDocumentAgentExtractionBackend : IDocumentAgentExtractionBackend
{
    public async Task<AgentExtractionResult> ExtractAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var extracted = await DocumentTextExtractionService.TryExtractAsync(buffer, fileName, cancellationToken);
        buffer.Position = 0;

        var text = extracted
            ?? $"Agent stub: no plain-text body in {Path.GetFileName(fileName)}. Enable Syncfusion AgentTools (Phase 10C-A2) for PDF/DOCX extraction.";

        return new AgentExtractionResult(
            text,
            DocumentAgentService.InferTableCount(fileName),
            UsedSyncfusionTools: false);
    }
}
