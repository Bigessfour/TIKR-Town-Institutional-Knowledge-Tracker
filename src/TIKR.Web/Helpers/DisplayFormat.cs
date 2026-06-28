namespace TIKR.Web.Helpers;

public static class DisplayFormat
{
    public static string[] ParseTags(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Array.Empty<string>();

        try
        {
            var arr = System.Text.Json.JsonSerializer.Deserialize<string[]>(raw);
            return arr ?? [raw];
        }
        catch
        {
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }

    public static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024):F1} MB";
    }

    public static string TruncateForDisplay(string content) =>
        content.Length > 180 ? content[..177] + "..." : content;
}
