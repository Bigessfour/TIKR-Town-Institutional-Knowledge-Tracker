using System.Text.RegularExpressions;

namespace TIKR.Shared.Helpers;

/// <summary>Lightweight heuristic extractors for durable clerk memory facts (MVP — no LLM).</summary>
public static partial class UserMemoryFactExtractor
{
    public static IReadOnlyList<(string Key, string Value)> Extract(string? userText)
    {
        if (string.IsNullOrWhiteSpace(userText))
            return [];

        var text = userText.Trim();
        var results = new List<(string Key, string Value)>();

        TryBirthday(text, results);
        TryPreferredName(text, results);
        TryRememberThat(text, results);

        return results;
    }

    public static string FormatForPrompt(IEnumerable<(string Key, string Value)> facts)
    {
        var list = facts
            .Where(f => !string.IsNullOrWhiteSpace(f.Key) && !string.IsNullOrWhiteSpace(f.Value))
            .Select(f => $"- {f.Key}: {f.Value.Trim()}")
            .ToList();
        if (list.Count == 0)
            return string.Empty;

        return "Known facts about this clerk (use when relevant; do not invent others):\n" +
               string.Join("\n", list);
    }

    private static void TryBirthday(string text, List<(string Key, string Value)> results)
    {
        var m = BirthdayRegex().Match(text);
        if (!m.Success)
            return;
        var value = m.Groups["value"].Value.Trim().TrimEnd('.', '!', '?');
        if (value.Length > 0)
            results.Add(("birthday", value));
    }

    private static void TryPreferredName(string text, List<(string Key, string Value)> results)
    {
        var m = PreferredNameRegex().Match(text);
        if (!m.Success)
            return;
        var value = m.Groups["name"].Value.Trim().TrimEnd('.', '!', '?');
        if (value.Length is > 0 and < 80)
            results.Add(("preferred_name", value));
    }

    private static void TryRememberThat(string text, List<(string Key, string Value)> results)
    {
        var m = RememberThatRegex().Match(text);
        if (!m.Success)
            return;
        var value = m.Groups["fact"].Value.Trim().TrimEnd('.', '!', '?');
        if (value.Length is > 0 and < 500)
            results.Add(("note", value));
    }

    [GeneratedRegex(
        @"\b(?:my\s+)?birthday\s+(?:is|was)\s+(?<value>[^.!?\n]{2,80})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BirthdayRegex();

    [GeneratedRegex(
        @"\b(?:please\s+)?(?:call\s+me|my\s+name\s+is)\s+(?<name>[A-Za-z][A-Za-z\-']{0,40})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PreferredNameRegex();

    [GeneratedRegex(
        @"\bremember\s+that\s+(?<fact>.{3,400})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RememberThatRegex();
}
