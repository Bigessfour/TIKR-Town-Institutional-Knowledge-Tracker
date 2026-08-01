namespace TIKR.Infrastructure.Services;

/// <summary>
/// Deterministic folder/tag suggestions when Ollama returns empty or incomplete tagging.
/// Prefer AI results; heuristics only fill gaps.
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
