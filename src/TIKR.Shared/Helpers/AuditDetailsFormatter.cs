using System.Text.Json;

namespace TIKR.Shared.Helpers;

/// <summary>Formats audit <c>Details</c> for clerk UI (plain text or JSON field diffs).</summary>
public static class AuditDetailsFormatter
{
    public static string Format(string? details)
    {
        if (string.IsNullOrWhiteSpace(details))
            return string.Empty;

        var trimmed = details.Trim();
        if (trimmed[0] != '{')
            return trimmed;

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            var root = doc.RootElement;
            var summary = root.TryGetProperty("summary", out var s) ? s.GetString() : null;
            if (!root.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Object)
                return summary ?? trimmed;

            var parts = new List<string>();
            foreach (var prop in changes.EnumerateObject())
            {
                var from = prop.Value.TryGetProperty("from", out var f) ? f.GetString() : null;
                var to = prop.Value.TryGetProperty("to", out var t) ? t.GetString() : null;
                parts.Add($"{prop.Name}: {Truncate(from)} → {Truncate(to)}");
            }

            if (parts.Count == 0)
                return summary ?? trimmed;

            var changeText = string.Join("; ", parts);
            return string.IsNullOrWhiteSpace(summary) ? changeText : $"{summary} ({changeText})";
        }
        catch (JsonException)
        {
            return trimmed;
        }
    }

    private static string Truncate(string? value, int max = 40)
    {
        if (string.IsNullOrEmpty(value))
            return "(empty)";
        return value.Length <= max ? value : value[..(max - 1)] + "…";
    }
}
