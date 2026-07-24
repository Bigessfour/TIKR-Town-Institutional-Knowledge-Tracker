using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
using TIKR.Web.Services;

namespace TIKR.Web.Helpers;

public static class AssistantPromptBuilder
{
    public static string BuildSystemPrompt(ColoradoResourceCatalog catalog)
    {
        const string basePrompt =
            "You are TIKR, a helpful AI assistant for a one-person Colorado municipal town clerk. " +
            "Answer concisely about deadlines, documents, procedures, and institutional knowledge. " +
            "When document or vault context is provided, answer ONLY from that context. " +
            "If the context is missing, empty, or does not contain the answer, say you do not have matching " +
            "documents or institutional knowledge in TIKR — do not invent procedures or fees. " +
            "When you use context, end with a Sources section listing the document filenames and vault titles used. " +
            "If unsure, say so and recommend the most relevant external source below by name and URL; " +
            "for binding legal questions, always advise consulting the town attorney.";

        var catalogBlock = catalog.ToSystemPromptBlock();
        if (string.IsNullOrWhiteSpace(catalogBlock))
            return basePrompt;

        return basePrompt +
            "\n\nTrusted external sources for Colorado municipal clerks (cite name + URL when referring users out):\n" +
            catalogBlock;
    }

    /// <summary>
    /// Packs retrieved passages for the chat model. Returns null when search is unavailable.
    /// </summary>
    public static string? FormatDocumentRagBlock(SemanticSearchResponse? search, out bool searchUnavailable)
    {
        searchUnavailable = search is { EmbeddingAvailable: false };
        if (search is null || searchUnavailable || search.Hits is not { Count: > 0 })
            return null;

        return "Relevant documents:\n" + string.Join("\n\n", search.Hits.Select(h =>
            $"- Source: {h.FileName}" +
            (string.IsNullOrWhiteSpace(h.SuggestedFolder) ? "" : $" [{h.SuggestedFolder}]") +
            (h.ChunkIndex is int idx ? $" (passage {idx + 1})" : "") +
            (string.IsNullOrWhiteSpace(h.Snippet) ? "" : $"\n  {h.Snippet}")));
    }

    public static string? FormatVaultRagBlock(SemanticSearchKnowledgeResponse? search, out bool searchUnavailable)
    {
        searchUnavailable = search is { EmbeddingAvailable: false };
        if (search is null || searchUnavailable || search.Hits is not { Count: > 0 })
            return null;

        return "Relevant institutional knowledge:\n" + string.Join("\n\n", search.Hits.Select(h =>
            $"- Source: {h.Title} [{h.Category}]" +
            (h.ChunkIndex is int idx ? $" (passage {idx + 1})" : "") +
            (string.IsNullOrWhiteSpace(h.Snippet) ? "" : $"\n  {h.Snippet}")));
    }

    public static IReadOnlyList<string> CollectCitationLabels(
        SemanticSearchResponse? docs,
        SemanticSearchKnowledgeResponse? vault)
    {
        var labels = new List<string>();
        if (docs?.Hits is { Count: > 0 })
            labels.AddRange(docs.Hits.Select(h => h.FileName).Where(n => !string.IsNullOrWhiteSpace(n)));
        if (vault?.Hits is { Count: > 0 })
            labels.AddRange(vault.Hits.Select(h => h.Title).Where(n => !string.IsNullOrWhiteSpace(n)));
        return labels.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static string BuildUserMessageWithRag(
        string question,
        string? deadlineContext,
        string? docContext,
        string? vaultContext,
        bool searchUnavailable,
        IReadOnlyList<string> citations)
    {
        var blocks = new List<string>();
        if (searchUnavailable)
            blocks.Add("Note: Document/vault search is temporarily unavailable (local embedding service offline). Answer only from deadlines below if present; otherwise say you cannot search TIKR knowledge right now.");
        if (!string.IsNullOrWhiteSpace(deadlineContext))
            blocks.Add($"Upcoming priorities:\n{deadlineContext}");
        if (!string.IsNullOrWhiteSpace(docContext))
            blocks.Add(docContext);
        if (!string.IsNullOrWhiteSpace(vaultContext))
            blocks.Add(vaultContext);
        if (citations.Count > 0)
            blocks.Add("Required Sources to cite if used:\n" + string.Join("\n", citations.Select(c => $"- {c}")));
        if (blocks.Count == 0)
            return question + "\n\n(No matching documents or vault entries were retrieved. If you cannot answer from general clerk practice, say so.)";
        return string.Join("\n\n", blocks) + $"\n\nQuestion: {question}";
    }

    public static string FormatDeadlineContext(IReadOnlyList<DashboardPriority> priorities) =>
        string.Join("\n", priorities.Select(p =>
            $"- {p.Title} ({p.Priority}): {p.Reason}" +
            (p.DueDate.HasValue ? $" — due {p.DueDate.Value:MMM d}" : "")));

    /// <summary>
    /// Plain-text streaming preview for SfAIAssistView. UpdateResponseAsync replaces the bubble;
    /// callers must pass the full accumulated markdown each time. Partial Markdig HTML mid-stream
    /// breaks incomplete fences/lists, so we HTML-encode until the final render.
    /// </summary>
    public static string FormatStreamingHtml(string markdown) =>
        $"<div class=\"tikr-assist-stream\">{System.Net.WebUtility.HtmlEncode(markdown)}</div>";
}

public static class VaultCopyBuilder
{
    public static string BuildCopyAllText(
        IEnumerable<KnowledgeEntryDto> howTo,
        IEnumerable<KnowledgeEntryDto> contacts,
        IEnumerable<KnowledgeEntryDto> tribal,
        IEnumerable<(string Title, DateTime Timestamp, string Transcription)> voiceNotes)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== TIKR KNOWLEDGE VAULT - FOR THE NEW CLERK ===");
        sb.AppendLine("If I'm gone, this has everything you need. Take care of the town.");
        sb.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        sb.AppendLine();

        AppendSection(sb, "HOW-TO", howTo);
        AppendSection(sb, "CONTACTS", contacts);
        AppendSection(sb, "TRIBAL KNOWLEDGE", tribal);

        sb.AppendLine("--- VOICE NOTES ---");
        foreach (var v in voiceNotes)
            sb.AppendLine($"• {v.Title} ({v.Timestamp:HH:mm})\n{v.Transcription}\n");

        return sb.ToString();
    }

    private static void AppendSection(System.Text.StringBuilder sb, string title, IEnumerable<KnowledgeEntryDto> entries)
    {
        sb.AppendLine($"--- {title} ---");
        foreach (var e in entries)
            sb.AppendLine($"• {e.Title}\n{e.Content}\n");
    }

    public static IEnumerable<KnowledgeEntryDto> FilterCategory(
        IEnumerable<KnowledgeEntryDto> entries,
        KnowledgeCategory category) =>
        entries.Where(e => e.Category == category).OrderBy(e => e.SortOrder);
}

public static class DocumentUiMessages
{
    public static string UploadSuccess(string fileName) => $"Uploaded and AI-analyzed: {fileName}";
    public static string UploadFailure(string fileName) => $"Failed to upload {fileName}";
    public static string BulkDelete(int count) => $"Deleted {count} document(s).";
    public static string BulkRetag(int count) => $"Re-tagged {count} document(s).";
    public static string SuggestionAccepted() => "Suggestion accepted.";
    public static string DownloadSuccess(string fileName) => $"Downloaded {fileName} from NAS storage.";
    public static string DownloadFailed(string fileName) => $"Could not download {fileName}. The file may be missing on the NAS.";
    public static string DownloadInProgress(string fileName) => $"Downloading {fileName} from NAS storage…";
    public static string DownloadLargeFileWarning(string fileName, long sizeBytes) =>
        $"{fileName} is {DisplayFormat.FormatBytes(sizeBytes)} — download may take a while on Synology.";
    public static string SemanticSearchFailed(string message) => $"Semantic search failed: {message}";

    public static string GenerationInProgress(string label) => $"Generating {label} on NAS…";

    public static string GenerationSuccess(string fileName) => $"Downloaded {fileName}.";

    public static string GenerationFailed(string? message) =>
        string.IsNullOrWhiteSpace(message)
            ? "Could not generate document. Check API and Syncfusion license on Settings."
            : message;

    public static string ConvertToPdfInProgress(string fileName) => $"Converting {fileName} to PDF…";

    public static string ConvertToPdfSuccess(string fileName) => $"Downloaded PDF converted from {fileName}.";

    public static string CouncilPacketBuilding() =>
        "Building council packet (cover page, deadlines table, linked documents)…";

    public static string CouncilPacketSaving() =>
        "Saving PDF and DOCX to Synology NAS storage…";

    public static string CouncilPacketSuccess(string pdfName, string docxName) =>
        $"Council packet saved to NAS: {pdfName} and {docxName}.";

    public static bool CanConvertToPdf(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension is ".doc" or ".docx" or ".xls" or ".xlsx"
            or ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".tiff" or ".tif";
    }

    public static string ExtractToVaultInProgress(string fileName) => $"Extracting text/tables from {fileName}…";
    public static string ExtractToVaultSuccess(string fileName) => $"Extracted text/tables from {fileName} into Knowledge Vault.";
}
