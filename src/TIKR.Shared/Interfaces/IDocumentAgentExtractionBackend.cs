namespace TIKR.Shared.Interfaces;

public record AgentExtractionResult(string ExtractedText, int TablesExtractedCount, bool UsedSyncfusionTools);

/// <summary>
/// Pluggable extraction for agent-scan. Stub heuristics today; Syncfusion AgentTools in 10C-A2+.
/// </summary>
public interface IDocumentAgentExtractionBackend
{
    Task<AgentExtractionResult> ExtractAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default);
}
