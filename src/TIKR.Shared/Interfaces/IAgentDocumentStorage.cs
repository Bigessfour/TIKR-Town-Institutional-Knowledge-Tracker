namespace TIKR.Shared.Interfaces;

/// <summary>
/// Persists agent-scan payloads on the NAS volume (optional AES at rest).
/// Syncfusion DocumentStorage mode maps here in Phase 10C-A3.
/// </summary>
public interface IAgentDocumentStorage
{
    Task<string> SaveAgentScanAsync(Stream content, string fileName, CancellationToken cancellationToken = default);
}
