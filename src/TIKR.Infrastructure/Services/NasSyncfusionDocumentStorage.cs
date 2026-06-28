using Syncfusion.AI.AgentTools.Core;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// NAS-local <see cref="IDocumentStorage"/> for Syncfusion Agent Tools Storage Mode.
/// Paths are relative to the TIKR file volume (agent-scans/sf-work/).
/// </summary>
public sealed class NasSyncfusionDocumentStorage(IFileStorageService fileStorage) : IDocumentStorage
{
    private const string Prefix = "agent-scans/sf-work/";

    public Stream Read(string filePath)
    {
        var relative = ToRelativePath(filePath);
        var stream = fileStorage.OpenReadAsync(relative).GetAwaiter().GetResult();
        var copy = new MemoryStream();
        stream.CopyTo(copy);
        stream.Dispose();
        copy.Position = 0;
        return copy;
    }

    public bool Write(string filePath, Stream documentStream)
    {
        var relative = ToRelativePath(filePath);
        fileStorage.SaveAsync(documentStream, relative).GetAwaiter().GetResult();
        return true;
    }

    public bool Exists(string filePath)
    {
        var full = fileStorage.GetFullPath(ToRelativePath(filePath));
        return File.Exists(full);
    }

    internal static string ToRelativePath(string filePath)
    {
        var trimmed = filePath.TrimStart('/', '\\').Replace('\\', '/');
        return trimmed.StartsWith(Prefix, StringComparison.Ordinal) ? trimmed : Prefix + trimmed;
    }
}
