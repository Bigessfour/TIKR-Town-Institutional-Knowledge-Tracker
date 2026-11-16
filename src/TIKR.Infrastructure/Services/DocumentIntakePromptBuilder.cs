namespace TIKR.Infrastructure.Services;

/// <summary>Builds structured JSON prompts for document intake (due dates, contacts, actions).</summary>
public static class DocumentIntakePromptBuilder
{
    public const float Temperature = 0.15f;
    public const int MaxExcerptChars = 10_000;

    private const string JsonShape =
        """{"dueDates":[{"date":"YYYY-MM-DD","label":"optional","sourceSnippet":"optional"}],"contacts":[{"name":"…","organization":"","email":"","phone":"","role":"","notes":""}],"actionItems":[{"title":"…","dueDate":"YYYY-MM-DD or null","notes":""}]}""";

    public static string Build(string fileName, string contentExcerpt)
    {
        var excerpt = contentExcerpt.Length <= MaxExcerptChars
            ? contentExcerpt
            : contentExcerpt[..MaxExcerptChars];

        return
            "You are the Town of Wiley (Colorado) municipal clerk assistant for TIKR. " +
            "Extract deadlines, points of contact, and follow-up action items from the document text. " +
            "Respond with JSON only (no markdown):\n" +
            JsonShape + "\n\n" +
            "Rules:\n" +
            "- Use ISO dates YYYY-MM-DD when possible.\n" +
            "- Omit empty arrays when nothing found.\n" +
            "- Prefer clerk-relevant filings (ordinances, elections, budgets, notices).\n" +
            "- actionItems are concrete next steps for the clerk (file, post, call, submit).\n\n" +
            $"File name: {fileName}\n" +
            $"Content:\n{excerpt}";
    }
}
