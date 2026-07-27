using Microsoft.Extensions.Configuration;
using TIKR.Shared.Configuration;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

public class LocalFileStorageService(FeatureSettingsState settings, IConfiguration configuration) : IFileStorageService
{
    private string BasePath
    {
        get
        {
            var fromSettings = settings.Current.FileStoragePath;
            if (!string.IsNullOrWhiteSpace(fromSettings))
                return fromSettings;
            return TikrConfiguration.GetFileStoragePath(configuration);
        }
    }

    public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var basePath = BasePath;
        Directory.CreateDirectory(basePath);

        var normalized = fileName.Replace('\\', '/').TrimStart('/');
        string relativePath;
        if (normalized.StartsWith("agent-scans/", StringComparison.Ordinal))
        {
            relativePath = normalized;
        }
        else
        {
            var safeName = Path.GetFileName(fileName);
            relativePath = Path.Combine(DateTime.UtcNow.ToString("yyyy/MM"), $"{Guid.NewGuid():N}_{safeName}");
        }

        var fullPath = GetFullPath(relativePath);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, cancellationToken);

        return relativePath.Replace('\\', '/');
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storagePath);
        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(storagePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    public string GetFullPath(string storagePath) =>
        Path.Combine(BasePath, storagePath.Replace('/', Path.DirectorySeparatorChar));
}
