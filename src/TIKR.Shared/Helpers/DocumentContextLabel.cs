using System.Text.Json;
using System.Text.RegularExpressions;

namespace TIKR.Shared.Helpers;

/// <summary>
/// Builds agent-facing document labels and short summaries for RAG / tool context.
/// Prefer content-derived topics over folder names so hits like "Scanned Document.pdf"
/// become "[Retirement Package Form DD-2656] Scanned Document.pdf".
/// </summary>
public static partial class DocumentContextLabel
{
    public const int DefaultTopicMaxLen = 96;
    public const int DefaultSummaryMaxLen = 240;
    public const int DefaultDisplayNameMaxLen = 500;

    /// <summary>
    /// Topic/title inferred from full text, tags, or a non-generic file stem.
    /// Returns null when nothing more descriptive than the file name is available.
    /// </summary>
    public static string? InferTopic(
        string? fileName,
        string? fullTextContent,
        string? aiTags = null,
        string? suggestedFolder = null,
        int maxLen = DefaultTopicMaxLen)
    {
        var fromText = InferTopicFromText(fullTextContent, maxLen);
        if (!string.IsNullOrWhiteSpace(fromText))
            return fromText;

        var fromTags = InferTopicFromTags(aiTags, maxLen);
        if (!string.IsNullOrWhiteSpace(fromTags))
            return fromTags;

        var stem = FileStem(fileName);
        if (!string.IsNullOrWhiteSpace(stem) && !IsGenericFileStem(stem))
            return Truncate(CleanLine(stem), maxLen);

        // Folder only as last resort when it is more specific than a generic bucket.
        if (!string.IsNullOrWhiteSpace(suggestedFolder) && !IsGenericFolder(suggestedFolder))
            return Truncate(CleanLine(suggestedFolder), maxLen);

        return null;
    }

    /// <summary>
    /// Label agents cite: <c>[Topic] FileName</c>, or just <paramref name="fileName"/> when no topic.
    /// </summary>
    public static string BuildSourceLabel(string? fileName, string? topic, int maxLen = DefaultDisplayNameMaxLen)
    {
        var name = string.IsNullOrWhiteSpace(fileName) ? "document" : fileName.Trim();
        if (string.IsNullOrWhiteSpace(topic))
            return Truncate(name, maxLen);

        var cleanedTopic = CleanLine(topic);
        if (string.IsNullOrWhiteSpace(cleanedTopic))
            return Truncate(name, maxLen);

        // Avoid "[Scanned Document] Scanned Document.pdf"
        if (TopicsMatchFileName(cleanedTopic, name))
            return Truncate(name, maxLen);

        var label = $"[{Truncate(cleanedTopic, DefaultTopicMaxLen)}] {name}";
        return Truncate(label, maxLen);
    }

    /// <summary>
    /// One- or two-sentence orientation excerpt from document body (not query-matched).
    /// </summary>
    public static string? BuildSummary(string? fullTextContent, int maxLen = DefaultSummaryMaxLen)
    {
        if (string.IsNullOrWhiteSpace(fullTextContent))
            return null;

        var text = NormalizeWhitespace(fullTextContent);
        if (text.Length == 0)
            return null;

        // Prefer first sentence-ish units
        var cut = text.Length;
        var seen = 0;
        for (var i = 0; i < text.Length && i < maxLen + 80; i++)
        {
            var c = text[i];
            if (c is '.' or '!' or '?')
            {
                seen++;
                if (seen >= 2 || i >= 80)
                {
                    cut = i + 1;
                    break;
                }
            }
        }

        if (cut > maxLen)
            cut = maxLen;

        var summary = text[..Math.Min(cut, text.Length)].Trim();
        if (summary.Length < 24)
        {
            summary = text[..Math.Min(maxLen, text.Length)].Trim();
        }

        if (summary.Length == 0)
            return null;

        if (text.Length > summary.Length)
            summary = summary.TrimEnd('.', ',', ';', ':', ' ') + "…";

        return summary;
    }

    /// <summary>
    /// Single-line source header for RAG / tool output.
    /// Example: <c>[Retirement Package Form DD-2656] Scanned Document.pdf — Correspondence (passage 1)</c>
    /// </summary>
    public static string FormatSourceHeader(
        string? fileName,
        string? topic,
        string? suggestedFolder = null,
        int? chunkIndex = null)
    {
        var label = BuildSourceLabel(fileName, topic);
        var parts = new List<string> { label };

        if (!string.IsNullOrWhiteSpace(suggestedFolder))
            parts.Add(suggestedFolder.Trim());

        if (chunkIndex is int idx)
            parts.Add($"passage {idx + 1}");

        if (parts.Count == 1)
            return parts[0];

        return $"{parts[0]} — {string.Join(" · ", parts.Skip(1))}";
    }

    /// <summary>
    /// Multi-line RAG / tool block for one hit (header + optional About + Excerpt).
    /// </summary>
    public static string FormatRagHit(
        string? fileName,
        string? topic,
        string? suggestedFolder,
        int? chunkIndex,
        string? summary,
        string? snippet)
    {
        var header = FormatSourceHeader(fileName, topic, suggestedFolder, chunkIndex);
        var lines = new List<string> { $"- Source: {header}" };

        if (!string.IsNullOrWhiteSpace(summary))
        {
            var about = summary.Trim();
            // Skip About when it is effectively the same as the query-matched excerpt
            if (string.IsNullOrWhiteSpace(snippet) || !SummariesOverlap(about, snippet))
                lines.Add($"  About: {about}");
        }

        if (!string.IsNullOrWhiteSpace(snippet))
            lines.Add($"  Excerpt: {snippet.Trim()}");

        return string.Join("\n", lines);
    }

    /// <summary>Citation label for Sources lists (topic-prefixed file name).</summary>
    public static string FormatCitationLabel(string? fileName, string? topic) =>
        BuildSourceLabel(fileName, topic);

    internal static string? InferTopicFromText(string? fullTextContent, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(fullTextContent))
            return null;

        var head = fullTextContent.Length > 1200 ? fullTextContent[..1200] : fullTextContent;
        var formId = FormIdRegex().Match(head);
        string? formPart = formId.Success ? CleanLine(formId.Value) : null;

        string? titleLine = null;
        foreach (var raw in head.Replace("\r\n", "\n").Split('\n'))
        {
            var line = CleanLine(raw);
            if (line.Length < 6 || line.Length > 140)
                continue;
            if (IsNoiseLine(line))
                continue;
            if (line.Count(char.IsLetter) < 4)
                continue;

            titleLine = line;
            break;
        }

        if (titleLine is null && formPart is null)
            return null;

        if (titleLine is not null && formPart is not null)
        {
            if (titleLine.Contains(formPart, StringComparison.OrdinalIgnoreCase))
                return Truncate(titleLine, maxLen);
            // "Form DD-2656 — Data for Payment of Retired Personnel"
            if (titleLine.StartsWith("form", StringComparison.OrdinalIgnoreCase))
                return Truncate($"{titleLine}", maxLen);
            return Truncate($"{titleLine} ({formPart})", maxLen);
        }

        return Truncate(titleLine ?? formPart!, maxLen);
    }

    private static string? InferTopicFromTags(string? aiTags, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(aiTags))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(aiTags);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return HumanizeTag(aiTags, maxLen);

            var tags = doc.RootElement.EnumerateArray()
                .Select(e => e.GetString())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t!.Trim())
                .Where(t => !IsWeakTag(t))
                .ToList();

            if (tags.Count == 0)
                return null;

            // Prefer multi-word / longer descriptive tags
            var best = tags
                .OrderByDescending(t => t.Contains(' ') || t.Contains('-') ? 1 : 0)
                .ThenByDescending(t => t.Length)
                .First();
            return Truncate(HumanizeTag(best, maxLen)!, maxLen);
        }
        catch (JsonException)
        {
            return HumanizeTag(aiTags, maxLen);
        }
    }

    private static string? HumanizeTag(string tag, int maxLen)
    {
        var t = CleanLine(tag.Trim('[', ']', '"', ' '));
        if (string.IsNullOrWhiteSpace(t) || IsWeakTag(t))
            return null;
        // "retirement-package" -> "Retirement package"
        t = t.Replace('_', ' ').Replace('-', ' ');
        t = CollapseSpaceRegex().Replace(t, " ").Trim();
        if (t.Length == 0)
            return null;
        if (char.IsLower(t[0]))
            t = char.ToUpperInvariant(t[0]) + t[1..];
        return Truncate(t, maxLen);
    }

    private static bool IsWeakTag(string tag)
    {
        var t = tag.Trim().ToLowerInvariant();
        return t is "uncategorized" or "unknown" or "other" or "misc" or "general"
            or "document" or "scan" or "scanned" or "pdf" or "file";
    }

    private static bool IsGenericFolder(string folder)
    {
        var f = folder.Trim().ToLowerInvariant();
        return f is "correspondence" or "misc" or "other" or "general" or "uncategorized"
            or "inbox" or "scans" or "documents" or "files";
    }

    private static bool IsGenericFileStem(string stem)
    {
        var s = stem.Trim().ToLowerInvariant();
        if (s is "scanned document" or "scanned" or "scan" or "document" or "untitled"
            or "image" or "img" or "photo" or "file" or "new document" or "doc")
            return true;
        if (GenericScanStemRegex().IsMatch(s))
            return true;
        return false;
    }

    private static bool IsNoiseLine(string line)
    {
        if (PageHeaderRegex().IsMatch(line))
            return true;
        if (line.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            line.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return true;
        // Pure form-control markers
        if (line is "OMB" or "APPROVED" or "CONFIDENTIAL")
            return true;
        return false;
    }

    private static bool TopicsMatchFileName(string topic, string fileName)
    {
        var stem = FileStem(fileName);
        if (string.IsNullOrWhiteSpace(stem))
            return false;
        return string.Equals(
            NormalizeForCompare(topic),
            NormalizeForCompare(stem),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string FileStem(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;
        try
        {
            return Path.GetFileNameWithoutExtension(fileName.Trim()) ?? string.Empty;
        }
        catch (ArgumentException)
        {
            return fileName.Trim();
        }
    }

    private static string CleanLine(string value) =>
        CollapseSpaceRegex().Replace(value.Trim().Trim('\uFEFF'), " ");

    private static string NormalizeWhitespace(string value) =>
        CollapseSpaceRegex().Replace(value.Replace('\r', ' ').Replace('\n', ' ').Trim(), " ");

    private static string NormalizeForCompare(string value) =>
        NormalizeWhitespace(value).ToLowerInvariant();

    private static bool SummariesOverlap(string summary, string snippet)
    {
        var a = NormalizeWhitespace(summary);
        var b = NormalizeWhitespace(snippet).TrimStart('…', '.', ' ');
        if (a.Length == 0 || b.Length == 0)
            return false;
        var probeLen = Math.Min(48, Math.Min(a.Length, b.Length));
        if (probeLen < 16)
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        return b.StartsWith(a[..probeLen], StringComparison.OrdinalIgnoreCase)
               || a.StartsWith(b[..probeLen], StringComparison.OrdinalIgnoreCase);
    }

    private static string Truncate(string value, int maxLen)
    {
        if (maxLen < 1 || value.Length <= maxLen)
            return value;
        if (maxLen <= 1)
            return value[..maxLen];
        return value[..(maxLen - 1)].TrimEnd() + "…";
    }

    [GeneratedRegex(@"\b(?:Form\s+)?(?:DD|SF|OF|W|I|SS|CMS|IRS)[-\s]?\d{1,5}(?:[-\s]?[A-Z0-9]+)?\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FormIdRegex();

    [GeneratedRegex(@"^\s*(page\s+\d+(\s+of\s+\d+)?|p\.?\s*\d+|continued|see reverse)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PageHeaderRegex();

    [GeneratedRegex(@"^(img|dsc|scan|image|doc|file|photo)[-_\s]?\d+$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GenericScanStemRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex CollapseSpaceRegex();
}
