using System.Text;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// MVP text extraction for plain-text uploads. PDF/DOCX deferred to Phase 9 preview / Phase 10 agent.
/// </summary>
public static class DocumentTextExtractionService
{
    private const int MaxExtractBytes = 512 * 1024;

    private static readonly HashSet<string> TextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".md", ".csv", ".log", ".json", ".xml", ".html", ".htm"
    };

    public static bool CanExtract(string fileName) =>
        TextExtensions.Contains(Path.GetExtension(fileName));

    public static async Task<string?> TryExtractAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        if (!CanExtract(fileName))
            return null;

        await using var limited = new MemoryStream();
        var buffer = new byte[8192];
        var total = 0;
        int read;
        while ((read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            total += read;
            if (total > MaxExtractBytes)
                break;

            await limited.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        if (limited.Length == 0)
            return null;

        var text = Encoding.UTF8.GetString(limited.ToArray());
        text = text.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }
}
