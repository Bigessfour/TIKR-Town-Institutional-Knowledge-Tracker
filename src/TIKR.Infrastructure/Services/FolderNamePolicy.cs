namespace TIKR.Infrastructure.Services;

/// <summary>
/// Folder labels are virtual. Names stay short and cannot include path characters.
/// </summary>
public static class FolderNamePolicy
{
    public const int MaxLength = 80;

    public static string? CanonicalOrNull(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var trimmed = string.Join(' ', name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (trimmed.Length is 0 or > MaxLength)
            return null;
        // "Budget / Finance" is a label. A slash without spaces is a path.
        if (trimmed.Contains('\\') || trimmed.IndexOfAny([':', '*', '?', '"', '<', '>', '|']) >= 0)
            return null;
        if (trimmed.Replace(" / ", " ", StringComparison.Ordinal).Contains('/'))
            return null;
        if (trimmed.Equals(DocumentFileManagerUncategorized, StringComparison.OrdinalIgnoreCase))
            return null;
        if (trimmed.Equals(DocumentTagHeuristics.General, StringComparison.OrdinalIgnoreCase))
            return DocumentTagHeuristics.General;

        return trimmed;
    }

    public static string? Match(string? name, IReadOnlyList<string> names)
    {
        var canonical = CanonicalOrNull(name);
        if (canonical is null)
            return null;
        return names.FirstOrDefault(candidate =>
            string.Equals(candidate, canonical, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// A new folder proposal. Allowed names and General are not proposals.
    /// </summary>
    public static string? ProposalOrNull(
        string? name,
        IReadOnlyList<string> allowed,
        IReadOnlyList<string> declined)
    {
        var canonical = CanonicalOrNull(name);
        if (canonical is null || canonical == DocumentTagHeuristics.General)
            return null;
        if (Match(canonical, allowed) is not null || Match(canonical, declined) is not null)
            return null;
        return canonical;
    }

    private const string DocumentFileManagerUncategorized = "Uncategorized";
}
