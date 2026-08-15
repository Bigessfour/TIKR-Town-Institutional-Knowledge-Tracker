namespace TIKR.Infrastructure.Services;

/// <summary>
/// Deterministic folder/tag suggestions when Ollama returns empty or incomplete tagging.
/// Prefer AI results; heuristics only fill gaps.
/// NAS library paths can seed <see cref="TryMapNasRelativePath"/> before the LLM.
/// </summary>
public static class DocumentTagHeuristics
{
    public const string PersonnelHr = "Personnel / HR";
    public const string BudgetFinance = "Budget / Finance";
    public const string Ordinances = "Ordinances";
    public const string Agenda = "Agenda";
    public const string Minutes = "Minutes";
    public const string Correspondence = "Correspondence";
    public const string Forms = "Forms";
    public const string Contracts = "Contracts";
    public const string General = "General";

    public static readonly string[] FolderVocabulary =
    [
        Ordinances,
        Agenda,
        Minutes,
        BudgetFinance,
        Correspondence,
        Forms,
        PersonnelHr,
        Contracts,
        General
    ];

    /// <summary>
    /// Maps the first NAS folder segment (and filename cues for council packets) onto the
    /// 9-folder vocabulary. Returns null when the path is not a confident match.
    /// </summary>
    public static string? TryMapNasRelativePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        var normalized = relativePath.Replace('\\', '/').Trim('/');
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // Need a folder + file; bare filenames are not a NAS-tree signal.
        if (segments.Length < 2)
            return null;

        var topKey = NormalizeNasSegment(segments[0]);
        var nameLower = segments[^1].ToLowerInvariant();

        if (topKey is "council meetings" or "council meeting")
        {
            if (nameLower.Contains("agenda", StringComparison.Ordinal))
                return Agenda;
            // Plural only — bare "minute" matches noise like last-minute-notes.pdf.
            if (nameLower.Contains("minutes", StringComparison.Ordinal))
                return Minutes;
            return null;
        }

        if (topKey is "ordinances" or "ordinance" or "municipal code")
            return Ordinances;
        if (topKey is "budget" or "finance" or "mill levy" or "milllevy")
            return BudgetFinance;
        if (topKey is "contracts" or "contract" or "agreements" or "agreement")
            return Contracts;
        if (topKey is "personnel" or "hr" or "human resources")
            return PersonnelHr;
        if (topKey is "correspondence" or "letters" or "letter")
            return Correspondence;
        if (topKey is "forms" or "form")
            return Forms;

        return null;
    }

    private static string NormalizeNasSegment(string segment) =>
        string.Join(' ', segment.Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries))
            .Trim()
            .ToLowerInvariant();

    public static (string[] Tags, string? Folder) FillGaps(
        string fileName,
        string? content,
        string[] tags,
        string? folder)
    {
        var haystack = $"{fileName}\n{content ?? ""}";
        var lower = haystack.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(folder))
            folder = InferFolder(lower);

        if (tags.Length == 0)
            tags = InferTags(lower);

        return (tags, folder);
    }

    internal static string? InferFolder(string lowerHaystack)
    {
        if (LooksLikeResume(lowerHaystack))
            return PersonnelHr;
        if (ContainsAny(lowerHaystack, "budget", "mill levy", "milllevy", "appropriation", "finance"))
            return BudgetFinance;
        if (ContainsAny(lowerHaystack, "ordinance", "municipal code", "codified"))
            return Ordinances;
        if (ContainsAny(lowerHaystack, "agenda", "meeting notice", "posted agenda"))
            return Agenda;
        if (ContainsAny(lowerHaystack, "minutes", "council meeting", "board meeting"))
            return Minutes;
        if (ContainsAny(lowerHaystack, "contract", "agreement", "mou ", "memorandum of understanding"))
            return Contracts;
        if (ContainsAny(lowerHaystack, "correspondence", "letter to", "memo from"))
            return Correspondence;
        if (ContainsAny(lowerHaystack, "application form", "request form", "permit form"))
            return Forms;
        if (ContainsAny(lowerHaystack, "personnel", "human resources", "hr ", "employment", "payroll"))
            return PersonnelHr;

        return null;
    }

    internal static string[] InferTags(string lowerHaystack)
    {
        if (LooksLikeResume(lowerHaystack))
            return ["resume", "personnel"];
        if (ContainsAny(lowerHaystack, "budget", "mill levy", "finance"))
            return ["budget", "finance"];
        if (ContainsAny(lowerHaystack, "ordinance"))
            return ["ordinance"];
        if (ContainsAny(lowerHaystack, "agenda"))
            return ["agenda", "council"];
        if (ContainsAny(lowerHaystack, "minutes"))
            return ["minutes"];
        if (ContainsAny(lowerHaystack, "contract", "agreement"))
            return ["contract"];
        return [];
    }

    private static bool LooksLikeResume(string lower) =>
        ContainsAny(lower, "resume", "curriculum vitae", "curriculum_vitae")
        || HasWord(lower, "cv");

    private static bool HasWord(string haystack, string word)
    {
        var idx = 0;
        while ((idx = haystack.IndexOf(word, idx, StringComparison.Ordinal)) >= 0)
        {
            var beforeOk = idx == 0 || !char.IsLetterOrDigit(haystack[idx - 1]);
            var after = idx + word.Length;
            var afterOk = after >= haystack.Length || !char.IsLetterOrDigit(haystack[after]);
            if (beforeOk && afterOk)
                return true;
            idx = after;
        }

        return false;
    }

    private static bool ContainsAny(string haystack, params string[] needles) =>
        needles.Any(n => haystack.Contains(n, StringComparison.Ordinal));
}
