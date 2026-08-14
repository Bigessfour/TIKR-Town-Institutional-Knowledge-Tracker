using System.Text.Json;
using System.Text.RegularExpressions;

namespace TIKR.Shared.Helpers;

/// <summary>Formats audit <c>Details</c> for clerk UI (plain text or JSON field diffs).</summary>
public static partial class AuditDetailsFormatter
{
    public static string Format(string? details)
    {
        if (string.IsNullOrWhiteSpace(details))
            return string.Empty;

        var trimmed = details.Trim();

        // Settings saves used to dump host/path keys into Details — never show those on Dashboard.
        if (LooksLikeFeatureSettingsDump(trimmed))
            return SummarizeFeatureSettingsDump(trimmed);

        if (trimmed[0] != '{')
            return Truncate(trimmed, 120);

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            var root = doc.RootElement;
            var summary = root.TryGetProperty("summary", out var s) ? s.GetString() : null;
            if (!root.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Object)
                return summary ?? Truncate(trimmed, 120);

            var parts = new List<string>();
            foreach (var prop in changes.EnumerateObject())
            {
                var from = prop.Value.TryGetProperty("from", out var f) ? f.GetString() : null;
                var to = prop.Value.TryGetProperty("to", out var t) ? t.GetString() : null;
                parts.Add($"{prop.Name}: {Truncate(from)} → {Truncate(to)}");
            }

            if (parts.Count == 0)
                return summary ?? Truncate(trimmed, 120);

            var changeText = string.Join("; ", parts);
            return string.IsNullOrWhiteSpace(summary) ? changeText : $"{summary} ({changeText})";
        }
        catch (JsonException)
        {
            return Truncate(trimmed, 120);
        }
    }

    /// <summary>Clerk-facing corpus banner text (avoid dumping long filename lists).</summary>
    public static string FormatCorpusAttention(IReadOnlyList<string>? items, int maxNames = 2)
    {
        if (items is null || items.Count == 0)
            return string.Empty;

        var reindex = items.FirstOrDefault(i => i.Contains("reindex", StringComparison.OrdinalIgnoreCase));
        var names = items
            .Where(i => !i.Contains("reindex", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(reindex))
            parts.Add(reindex);

        if (names.Count > 0)
        {
            var shown = string.Join(", ", names.Take(maxNames));
            var extra = names.Count - maxNames;
            parts.Add(extra > 0
                ? $"{names.Count} scanned files need richer text for search (e.g. {shown} +{extra} more)"
                : $"{names.Count} scanned file(s) need richer text for search: {shown}");
        }

        return string.Join(" · ", parts);
    }

    private static bool LooksLikeFeatureSettingsDump(string details) =>
        details.Contains("OllamaHost=", StringComparison.OrdinalIgnoreCase)
        || details.Contains("FileStoragePath=", StringComparison.OrdinalIgnoreCase)
        || (details.Contains("Storage=", StringComparison.OrdinalIgnoreCase)
            && details.Contains("SyncfusionConfigured=", StringComparison.OrdinalIgnoreCase));

    private static string SummarizeFeatureSettingsDump(string details)
    {
        var grokOn = FeatureGrokOnRegex().IsMatch(details);
        return grokOn
            ? "Town helper settings saved (Advanced AI on)"
            : "Town helper settings saved";
    }

    private static string Truncate(string? value, int max = 40)
    {
        if (string.IsNullOrEmpty(value))
            return "(empty)";
        return value.Length <= max ? value : value[..(max - 1)] + "…";
    }

    [GeneratedRegex(@"UseGrok\s*=\s*True", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FeatureGrokOnRegex();
}
