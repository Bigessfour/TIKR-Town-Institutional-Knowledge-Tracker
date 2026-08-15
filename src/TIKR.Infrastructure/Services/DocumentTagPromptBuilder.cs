namespace TIKR.Infrastructure.Services;

/// <summary>
/// Builds few-shot tagging prompts for municipal clerk document classification.
/// Kept separate from HybridAiService so unit tests can assert content without Ollama.
/// </summary>
public static class DocumentTagPromptBuilder
{
    /// <summary>Low temperature for deterministic JSON tagging (~0.1–0.2).</summary>
    public const float TaggingTemperature = 0.15f;

    private const string JsonShape = """{"tags": ["tag1","tag2"], "suggestedFolder": "folder name"}""";

    /// <param name="fileName">Leaf file name (fallback when no library path).</param>
    /// <param name="contentPreview">Extracted text preview.</param>
    /// <param name="libraryRelativePath">
    /// NAS-relative path when imported via library scan (e.g. <c>COUNCIL MEETINGS/2024/agenda.pdf</c>).
    /// When set, the model sees the full path so folder labels inform classification.
    /// </param>
    public static string Build(string fileName, string contentPreview, string? libraryRelativePath = null)
    {
        var folders = string.Join(", ", DocumentTagHeuristics.FolderVocabulary);
        var displayName = string.IsNullOrWhiteSpace(libraryRelativePath)
            ? fileName
            : libraryRelativePath.Replace('\\', '/');

        return
            "You are the Town of Wiley (Prowers County, Colorado) municipal clerk assistant for TIKR. " +
            "Classify Board of Trustees and town hall filings. Respond with JSON only (no markdown, no commentary):\n" +
            JsonShape + "\n\n" +
            $"Choose suggestedFolder from this list when possible: {folders}.\n\n" +
            "Town context: Town of Wiley, Board of Trustees, Wiley School District (WSD), " +
            "Town Hall at 304 Main Street; regular meetings typically the 2nd Monday.\n\n" +
            "Examples:\n\n" +
            "File name: COUNCIL MEETINGS/2024-08-12 minutes.pdf\n" +
            "Content preview: Minutes of the Town of Wiley Board of Trustees regular meeting held August 12, 2024 at Town Hall, 304 Main Street.\n" +
            "Response: {\"tags\": [\"minutes\",\"board of trustees\"], \"suggestedFolder\": \"Minutes\"}\n\n" +
            "File name: COUNCIL MEETINGS/2025-03-10 agenda.pdf\n" +
            "Content preview: Agenda — Town of Wiley Board of Trustees, 2nd Monday meeting, Town Hall 304 Main Street.\n" +
            "Response: {\"tags\": [\"agenda\",\"council\"], \"suggestedFolder\": \"Agenda\"}\n\n" +
            "File name: BUDGET/2026 mill levy certification.pdf\n" +
            "Content preview: Town of Wiley FY2026 adopted budget and mill levy certification for Prowers County.\n" +
            "Response: {\"tags\": [\"budget\",\"mill levy\"], \"suggestedFolder\": \"Budget / Finance\"}\n\n" +
            "File name: ORDINANCES/Ordinance_2024-05.pdf\n" +
            "Content preview: An ordinance of the Town of Wiley amending Title 5 of the municipal code regarding parking.\n" +
            "Response: {\"tags\": [\"ordinance\"], \"suggestedFolder\": \"Ordinances\"}\n\n" +
            "Now classify this document:\n\n" +
            $"File name: {displayName}\n" +
            $"Content preview: {contentPreview}";
    }
}
