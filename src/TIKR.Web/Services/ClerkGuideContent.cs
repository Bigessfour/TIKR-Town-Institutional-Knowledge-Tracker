using System.Text.RegularExpressions;
using Markdig;

namespace TIKR.Web.Services;

public sealed record ClerkGuideSection(string Id, string Title, string BodyMarkdown);

public static class ClerkGuideContent
{
    private static readonly Regex HeadingRegex = new(@"^##\s+(.+)$", RegexOptions.Multiline);

    public static IReadOnlyList<ClerkGuideSection> ParseSections(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return [];

        var matches = HeadingRegex.Matches(markdown).Cast<Match>().ToList();
        if (matches.Count == 0)
        {
            return
            [
                new ClerkGuideSection("guide", "Guide", markdown.Trim()),
            ];
        }

        var sections = new List<ClerkGuideSection>();
        for (var i = 0; i < matches.Count; i++)
        {
            var title = matches[i].Groups[1].Value.Trim();
            var start = matches[i].Index + matches[i].Length;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : markdown.Length;
            var body = markdown[start..end].Trim();
            sections.Add(new ClerkGuideSection(Slugify(title), title, body));
        }

        return sections;
    }

    public static string ToHtml(string markdown) =>
        Markdown.ToHtml(markdown);

    public static IReadOnlyList<ClerkGuideSection> Filter(
        IReadOnlyList<ClerkGuideSection> sections,
        string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return sections;

        var q = query.Trim();
        return sections
            .Where(s => s.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                        || s.BodyMarkdown.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static string Slugify(string title)
    {
        var slug = Regex.Replace(title.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "section" : slug;
    }
}
