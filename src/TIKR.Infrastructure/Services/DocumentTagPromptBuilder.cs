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

    public static string Build(string fileName, string contentPreview)
    {
        var folders = string.Join(", ", DocumentTagHeuristics.FolderVocabulary);
        return
            "You are a Colorado municipal town clerk assistant. Classify the document and respond with JSON only (no markdown, no commentary):\n" +
            JsonShape + "\n\n" +
            $"Choose suggestedFolder from this list when possible: {folders}.\n\n" +
            "Examples:\n\n" +
            "File name: Stephen_Resume.pdf\n" +
            "Content preview: Curriculum Vitae — Software engineer with municipal experience.\n" +
            "Response: {\"tags\": [\"resume\",\"personnel\"], \"suggestedFolder\": \"Personnel / HR\"}\n\n" +
            "File name: budget-2026.pdf\n" +
            "Content preview: Town of Example FY2026 adopted budget and mill levy schedule.\n" +
            "Response: {\"tags\": [\"budget\",\"finance\"], \"suggestedFolder\": \"Budget / Finance\"}\n\n" +
            "File name: council-minutes-2026-03-12.pdf\n" +
            "Content preview: Minutes of the Board of Trustees regular meeting held March 12, 2026.\n" +
            "Response: {\"tags\": [\"minutes\"], \"suggestedFolder\": \"Minutes\"}\n\n" +
            "File name: Ordinance_12.pdf\n" +
            "Content preview: An ordinance amending Title 5 of the municipal code regarding parking.\n" +
            "Response: {\"tags\": [\"ordinance\"], \"suggestedFolder\": \"Ordinances\"}\n\n" +
            "Now classify this document:\n\n" +
            $"File name: {fileName}\n" +
            $"Content preview: {contentPreview}";
    }
}
