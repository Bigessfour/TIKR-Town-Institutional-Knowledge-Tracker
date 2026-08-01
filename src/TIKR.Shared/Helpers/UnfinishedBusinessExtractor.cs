namespace TIKR.Shared.Helpers;

/// <summary>Keyword/heuristic extraction of carry-over items from prior meeting minutes text.</summary>
public static class UnfinishedBusinessExtractor
{
    private static readonly string[] SignalPhrases =
    [
        "tabled",
        "table until",
        "continued to",
        "continue to",
        "postponed",
        "postpone until",
        "carried over",
        "carry over",
        "unfinished business",
        "old business",
        "refer to",
        "referred to",
        "held over",
        "deferred",
        "next meeting"
    ];

    public static IReadOnlyList<(string Title, string Quote)> Extract(string? fullText, int maxItems = 8)
    {
        if (string.IsNullOrWhiteSpace(fullText))
            return [];

        var results = new List<(string Title, string Quote)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in SplitLines(fullText))
        {
            if (results.Count >= maxItems)
                break;

            var trimmed = line.Trim();
            if (trimmed.Length < 12)
                continue;

            var lower = trimmed.ToLowerInvariant();
            if (!SignalPhrases.Any(p => lower.Contains(p, StringComparison.Ordinal)))
                continue;

            var title = TrimToTitle(trimmed);
            if (title.Length < 8 || !seen.Add(title))
                continue;

            results.Add((title, trimmed));
        }

        return results;
    }

    internal static IEnumerable<string> SplitLines(string text) =>
        text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    internal static string TrimToTitle(string line)
    {
        var title = line;
        if (title.Length > 120)
            title = title[..117] + "...";

        return title;
    }
}
