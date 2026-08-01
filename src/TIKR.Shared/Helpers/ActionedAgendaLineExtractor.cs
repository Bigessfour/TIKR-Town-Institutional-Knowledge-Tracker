using System.Text.RegularExpressions;

namespace TIKR.Shared.Helpers;

/// <summary>Extracts clerk-editable agenda lines from a linked actioned agenda document body.</summary>
public static partial class ActionedAgendaLineExtractor
{
    private static readonly string[] SkipContains =
    [
        "order of business",
        "town of wiley",
        "wiley sanitation",
        "regular meeting agenda",
        "board of trustees",
        "page ",
        "c.r.s.",
        "304 main street"
    ];

    public static IReadOnlyList<string> Extract(string? fullText, int maxItems = 40)
    {
        if (string.IsNullOrWhiteSpace(fullText))
            return [];

        var results = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in UnfinishedBusinessExtractor.SplitLines(fullText))
        {
            if (results.Count >= maxItems)
                break;

            var line = NormalizeLine(rawLine);
            if (line.Length < 4 || !seen.Add(line))
                continue;

            if (ShouldSkip(line))
                continue;

            results.Add(line);
        }

        return results;
    }

    internal static string NormalizeLine(string line)
    {
        var trimmed = line.Trim();
        trimmed = LeadingMarker().Replace(trimmed, string.Empty).Trim();
        trimmed = CollapseSpaces().Replace(trimmed, " ");
        return trimmed;
    }

    internal static bool ShouldSkip(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return true;

        var lower = line.ToLowerInvariant();
        if (SkipContains.Any(s => lower.Contains(s, StringComparison.Ordinal)))
            return true;

        if (DateOnly.TryParse(line, out _))
            return true;

        return lower is "agenda" or "minutes" or "adjourn";
    }

    [GeneratedRegex(@"^(\d+[\.\)]\s*|[ivxlcdm]+[\.\)]\s*|[-•*]\s*)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LeadingMarker();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex CollapseSpaces();
}
