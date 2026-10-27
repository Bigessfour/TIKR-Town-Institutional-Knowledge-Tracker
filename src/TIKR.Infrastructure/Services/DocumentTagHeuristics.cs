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

        if (SegmentHasWord(topKey, "ordinance", "ordinances") || topKey is "municipal code")
            return Ordinances;
        if (SegmentHasWord(topKey, "budget") || topKey is "finance" or "mill levy" or "milllevy")
            return BudgetFinance;
        if (topKey is "contracts" or "contract" or "agreements" or "agreement")
            return Contracts;
        if (topKey is "personnel" or "hr" or "human resources" or "employee info")
            return PersonnelHr;
        if (topKey is "correspondence" or "letters" or "letter")
            return Correspondence;
        if (topKey is "forms" or "form")
            return Forms;
        if (topKey is "election information" or "elections" or "election")
            return General;

        // Election is not one of the nine folders. Lock General so the chat model
        // cannot replace it (the 1B model filed this tree under Personnel / HR).
        // Whole words only — "selection" is not an election folder.
        if (SegmentHasWord(topKey, "election", "elections"))
            return General;

        return null;
    }

    /// <summary>
    /// Top library folder that is not one of the known roots. The clerk already named it.
    /// Known roots stay out of this so a council packet is not proposed as a new folder.
    /// </summary>
    public static string? TryReadUnmappedTopFolder(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;

        if (TryMapNasRelativePath(relativePath) is not null)
            return null;

        var normalized = relativePath.Replace('\\', '/').Trim('/');
        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
            return null;

        if (IsKnownLibraryRoot(NormalizeNasSegment(segments[0])))
            return null;

        return FolderNamePolicy.CanonicalOrNull(segments[0]);
    }

    internal static bool IsKnownLibraryRoot(string topKey) =>
        topKey is "council meetings" or "council meeting"
            or "ordinances" or "ordinance" or "municipal code"
            or "budget" or "finance" or "mill levy" or "milllevy"
            or "contracts" or "contract" or "agreements" or "agreement"
            or "personnel" or "hr" or "human resources" or "employee info"
            or "correspondence" or "letters" or "letter"
            or "forms" or "form"
            or "election information" or "elections" or "election"
        || SegmentHasWord(topKey, "election", "elections")
        || SegmentHasWord(topKey, "ordinance", "ordinances")
        || SegmentHasWord(topKey, "budget");

    private static bool SegmentHasWord(string topKey, params string[] words)
    {
        var parts = topKey.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Any(part => words.Contains(part, StringComparer.Ordinal));
    }

    private static string NormalizeNasSegment(string segment) =>
        string.Join(' ', segment.Split([' ', '_', '-', '&'], StringSplitOptions.RemoveEmptyEntries))
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

        // Filename only. Body text that mentions an election must not steal a
        // payroll or personnel file when the model left the folder empty.
        if (string.IsNullOrWhiteSpace(folder) && FileNameMentionsElection(fileName))
        {
            folder = General;
            if (tags.Length == 0)
                tags = ["election"];
        }

        return (tags, folder);
    }

    /// <summary>
    /// Path seed already replaced the model folder. Drop a personnel tag the model
    /// attached to an election filename, and keep an election tag.
    /// </summary>
    public static string[] TagsForLockedFolder(string fileName, string? lockedFolder, string[] tags)
    {
        if (lockedFolder != General || !FileNameMentionsElection(fileName))
            return tags;

        var kept = tags.Where(tag => !IsPersonnelTag(tag)).ToArray();
        if (!kept.Any(tag => tag.Equals("election", StringComparison.OrdinalIgnoreCase)))
            kept = [.. kept, "election"];
        return kept;
    }

    private static bool FileNameMentionsElection(string? fileName)
    {
        var lower = fileName?.ToLowerInvariant() ?? "";
        return HasWord(lower, "election") || HasWord(lower, "elections");
    }

    private static bool IsPersonnelTag(string tag) =>
        tag.Equals("personnel", StringComparison.OrdinalIgnoreCase)
        || tag.Equals("hr", StringComparison.OrdinalIgnoreCase)
        || tag.Equals("human resources", StringComparison.OrdinalIgnoreCase);

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
