using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// MVP stub: persists agent scan payloads via the NAS-local file volume.
/// Full AES encryption lands in Phase 10 group A.
/// </summary>
public static class SynologyDocumentStorage
{
    public static async Task<string> SaveAgentScanAsync(
        IFileStorageService storage,
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return await storage.SaveAsync(buffer, fileName, cancellationToken);
    }
}
