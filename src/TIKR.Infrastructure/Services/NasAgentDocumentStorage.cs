using Microsoft.Extensions.Configuration;
using TIKR.Shared.Configuration;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// NAS-local agent scan storage under agent-scans/. Optional AES-256-GCM when TIKR_AGENT_STORAGE_KEY is set.
/// </summary>
public class NasAgentDocumentStorage(IFileStorageService storage, IConfiguration configuration) : IAgentDocumentStorage
{
    public async Task<string> SaveAgentScanAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        var safeName = Path.GetFileName(fileName);
        if (AgentStorageCrypto.TryParseKey(TikrConfiguration.GetAgentStorageKey(configuration), out var key))
        {
            var encrypted = AgentStorageCrypto.Encrypt(bytes, key);
            await using var encryptedStream = new MemoryStream(encrypted);
            return await storage.SaveAsync(encryptedStream, $"agent-scans/{safeName}.agentenc", cancellationToken);
        }

        await using var plainStream = new MemoryStream(bytes);
        return await storage.SaveAsync(plainStream, $"agent-scans/{safeName}", cancellationToken);
    }
}
