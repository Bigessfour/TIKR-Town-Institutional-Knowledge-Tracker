using System.Text.RegularExpressions;
using Markdig;
using Microsoft.Extensions.AI;
using Syncfusion.Blazor.InteractiveChat;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
using TIKR.Shared.Helpers;
using TIKR.Web.Services;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace TIKR.Web.Helpers;

public static partial class AssistantPromptBuilder
{
    /// <summary>Max prior user+assistant pairs kept in the Ollama request (DB stores full thread).</summary>
    public const int DefaultMaxHistoryTurns = 8;

    public static string BuildSystemPrompt(
        ColoradoResourceCatalog catalog,
        IEnumerable<(string Key, string Value)>? memoryFacts = null)
    {
        const string basePrompt =
            "You are TIKR, a helpful AI assistant for a one-person Colorado municipal town clerk. " +
            "Answer concisely about deadlines, documents, procedures, and institutional knowledge. " +
            "When document or vault context is provided, answer ONLY from that context. " +
            "Document hits are labeled with a content topic when known " +
            "(e.g. [Retirement Package Form DD-2656] Scanned Document.pdf), an About summary, and an Excerpt. " +
            "Use the topic and About line to identify which file is relevant before relying on the Excerpt. " +
            "If the context is missing, empty, or does not contain the answer, say you do not have matching " +
            "documents or institutional knowledge in TIKR — do not invent procedures or fees. " +
            "When you use context, end with a Sources section listing the topic-labeled document names and vault titles used. " +
            "If unsure, say so and recommend the most relevant external source below by name and URL; " +
            "for binding legal questions, always advise consulting the town attorney. " +
            "Output ONLY the final clerk-facing answer. Do not write chain-of-thought, analysis steps, " +
            "planning, tool calls, function calls, XML/HTML control tags, or scratchpad lines " +
            "(no <think>, Thought:, Action:, FunctionCall, or JSON tool payloads).";

        var parts = new List<string> { basePrompt };

        var memoryBlock = UserMemoryFactExtractor.FormatForPrompt(memoryFacts ?? []);
        if (!string.IsNullOrWhiteSpace(memoryBlock))
            parts.Add(memoryBlock);

        var catalogBlock = catalog.ToSystemPromptBlock();
        if (!string.IsNullOrWhiteSpace(catalogBlock))
        {
            parts.Add(
                "Trusted external sources for Colorado municipal clerks (cite name + URL when referring users out):\n" +
                catalogBlock);
        }

        return string.Join("\n\n", parts);
    }

    /// <summary>Rebuild in-memory MEAI history from persisted plain turns (skips RAG-packed junk).</summary>
    public static List<ChatMessage> HistoryFromPersistedMessages(
        IEnumerable<ChatMessageDto> messages,
        int maxTurns = DefaultMaxHistoryTurns)
    {
        var history = new List<ChatMessage>();
        foreach (var msg in messages.OrderBy(m => m.CreatedAtUtc))
        {
            if (string.IsNullOrWhiteSpace(msg.Content))
                continue;
            if (LooksLikeRagPackedUserMessage(msg.Content))
                continue;

            var role = msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
                ? ChatRole.Assistant
                : ChatRole.User;
            history.Add(new ChatMessage(role, msg.Content));
        }

        TrimToMaxTurns(history, maxTurns);
        return history;
    }

    public static List<AssistViewPrompt> AssistViewPromptsFromPersisted(
        IEnumerable<ChatMessageDto> messages)
    {
        var prompts = new List<AssistViewPrompt>();
        string? pendingUser = null;
        foreach (var msg in messages.OrderBy(m => m.CreatedAtUtc))
        {
            if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
            {
                pendingUser = msg.Content;
                continue;
            }

            if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) && pendingUser is not null)
            {
                var html = Markdown.ToHtml(msg.Content ?? string.Empty);
                prompts.Add(new AssistViewPrompt { Prompt = pendingUser, Response = html });
                pendingUser = null;
            }
        }

        return prompts;
    }

    /// <summary>
    /// Packs retrieved passages for the chat model with topic labels + About + Excerpt.
    /// Returns null when search is unavailable or empty.
    /// </summary>
    public static string? FormatDocumentRagBlock(SemanticSearchResponse? search, out bool searchUnavailable)
    {
        searchUnavailable = search is { EmbeddingAvailable: false };
        if (search is null || searchUnavailable || search.Hits is not { Count: > 0 })
            return null;

        return "Relevant documents (topic label · folder · passage; About = document orientation; Excerpt = matched text):\n" +
               string.Join("\n\n", search.Hits.Select(h =>
                   DocumentContextLabel.FormatRagHit(
                       h.FileName,
                       h.Topic,
                       h.SuggestedFolder,
                       h.ChunkIndex,
                       h.Summary,
                       h.Snippet)));
    }

    public static string? FormatVaultRagBlock(SemanticSearchKnowledgeResponse? search, out bool searchUnavailable)
    {
        searchUnavailable = search is { EmbeddingAvailable: false };
        if (search is null || searchUnavailable || search.Hits is not { Count: > 0 })
            return null;

        return "Relevant institutional knowledge:\n" + string.Join("\n\n", search.Hits.Select(h =>
            $"- Source: {h.Title} — {h.Category}" +
            (h.ChunkIndex is int idx ? $" · passage {idx + 1}" : "") +
            (string.IsNullOrWhiteSpace(h.Snippet) ? "" : $"\n  Excerpt: {h.Snippet}")));
    }

    public static IReadOnlyList<string> CollectCitationLabels(
        SemanticSearchResponse? docs,
        SemanticSearchKnowledgeResponse? vault)
    {
        var labels = new List<string>();
        if (docs?.Hits is { Count: > 0 })
        {
            labels.AddRange(docs.Hits
                .Select(h => DocumentContextLabel.FormatCitationLabel(h.FileName, h.Topic))
                .Where(n => !string.IsNullOrWhiteSpace(n)));
        }
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
        {
            var line = $"- {p.Title} ({p.Priority}): {p.Reason}" +
                       (p.DueDate.HasValue ? $" — due {p.DueDate.Value:MMM d}" : "");
            var contact = FormatDueOutContactLine(p.SubmitTo, p.ContactName, p.ContactEmail, p.ContactPhone);
            return string.IsNullOrEmpty(contact) ? line : $"{line}\n  {contact}";
        }));

    /// <summary>One-line submit/contact summary for Assistant clerk context and calendar grids.</summary>
    public static string FormatDueOutContactLine(
        string? submitTo,
        string? contactName,
        string? contactEmail,
        string? contactPhone)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(submitTo))
            parts.Add($"Submit to: {submitTo.Trim()}");
        if (!string.IsNullOrWhiteSpace(contactName))
            parts.Add($"Contact: {contactName.Trim()}");
        if (!string.IsNullOrWhiteSpace(contactEmail))
            parts.Add(contactEmail.Trim());
        if (!string.IsNullOrWhiteSpace(contactPhone))
            parts.Add(contactPhone.Trim());
        return parts.Count == 0 ? string.Empty : string.Join(" · ", parts);
    }

    /// <summary>
    /// HTML-encode plain text for rare progressive previews. Prefer full-buffer + final markdown
    /// HTML: Syncfusion UpdateResponseAsync is streaming-oriented and mid-stream partials flash junk.
    /// </summary>
    public static string FormatStreamingHtml(string markdown) =>
        $"<div class=\"tikr-assist-stream\">{System.Net.WebUtility.HtmlEncode(markdown)}</div>";

    /// <summary>
    /// Stable clerk-facing placeholder while Ollama is generating (hides think/tool tokens).
    /// Matches Syncfusion AssistView guidance: show a loading state, not raw model scratchpad.
    /// </summary>
    public static string FormatPreparingHtml(string? message = null)
    {
        var text = string.IsNullOrWhiteSpace(message)
            ? "Preparing your answer…"
            : message.Trim();
        return $"<div class=\"tikr-assist-preparing\" role=\"status\" aria-live=\"polite\">" +
               $"<span class=\"tikr-assist-preparing-dot\" aria-hidden=\"true\"></span>" +
               $"{System.Net.WebUtility.HtmlEncode(text)}</div>";
    }

    /// <summary>
    /// Progressive UI text: strips thinking/tool scratchpads. While an open think/tool block is
    /// still incomplete, returns empty so the bubble can keep showing "Preparing…".
    /// </summary>
    public static string ExtractVisibleStreamingText(string rawAccumulated) =>
        SanitizeModelOutput(rawAccumulated, streamingIncomplete: true);

    /// <summary>
    /// Final clerk-facing markdown: remove chain-of-thought, tool/function call blocks, and agent lines.
    /// </summary>
    public static string SanitizeModelOutput(string? raw, bool streamingIncomplete = false)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var text = raw;

        // Normalize exotic think delimiters used by some Ollama models
        text = text.Replace("◁think▷", "<think>", StringComparison.Ordinal);
        text = text.Replace("◁/think▷", "</think>", StringComparison.Ordinal);
        text = text.Replace("<|begin_of_thought|>", "<think>", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("<|end_of_thought|>", "</think>", StringComparison.OrdinalIgnoreCase);
        text = text.Replace("<|begin_of_solution|>", string.Empty, StringComparison.OrdinalIgnoreCase);
        text = text.Replace("<|end_of_solution|>", string.Empty, StringComparison.OrdinalIgnoreCase);

        // Complete thinking / reflection blocks (DeepSeek-R1, Qwen, etc.)
        text = ThinkBlockRegex().Replace(text, string.Empty);
        text = ThinkingBlockRegex().Replace(text, string.Empty);
        text = ReflectionBlockRegex().Replace(text, string.Empty);
        text = RedactedReasoningBlockRegex().Replace(text, string.Empty);
        text = RedactedThinkingBlockRegex().Replace(text, string.Empty);

        // Tool / function call payloads models sometimes stream as text
        text = ToolCallBlockRegex().Replace(text, string.Empty);
        text = FunctionCallBlockRegex().Replace(text, string.Empty);
        text = ToolCodeFenceRegex().Replace(text, string.Empty);
        text = InlineJsonToolRegex().Replace(text, string.Empty);

        if (streamingIncomplete)
        {
            // Incomplete open tags — hide remainder until the model closes the block
            text = IncompleteThinkOpenRegex().Replace(text, string.Empty);
            text = IncompleteToolOpenRegex().Replace(text, string.Empty);
            text = IncompleteFenceOpenRegex().Replace(text, string.Empty);
        }

        // ReAct-style scratchpad lines until a Final Answer / plain answer
        text = StripAgentScratchpadLines(text);

        // Orphan special tokens some Ollama models leak
        text = SpecialTokenRegex().Replace(text, string.Empty);

        // Collapse leftover blank runs after stripping
        text = CollapseBlankLinesRegex().Replace(text, "\n\n");

        return text.Trim();
    }

    private static string StripAgentScratchpadLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Prefer content after "Final Answer:" when present
        var finalIdx = text.LastIndexOf("Final Answer:", StringComparison.OrdinalIgnoreCase);
        if (finalIdx >= 0)
        {
            var after = text[(finalIdx + "Final Answer:".Length)..].TrimStart();
            if (!string.IsNullOrWhiteSpace(after))
                return after;
        }

        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var kept = new List<string>(lines.Length);
        var inScratch = false;
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (AgentScratchpadLineRegex().IsMatch(trimmed))
            {
                inScratch = true;
                continue;
            }

            // End scratchpad when we hit a normal sentence after agent lines
            if (inScratch)
            {
                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;
                if (trimmed.StartsWith("Final Answer", StringComparison.OrdinalIgnoreCase))
                {
                    inScratch = false;
                    var colon = trimmed.IndexOf(':');
                    kept.Add(colon >= 0 ? trimmed[(colon + 1)..].TrimStart() : trimmed);
                    continue;
                }

                // Still looks like agent meta → skip
                if (trimmed.StartsWith('{') || trimmed.StartsWith('[') ||
                    trimmed.Contains("\"name\"", StringComparison.Ordinal) ||
                    trimmed.Contains("\"arguments\"", StringComparison.Ordinal))
                    continue;

                inScratch = false;
            }

            kept.Add(line);
        }

        return string.Join('\n', kept);
    }

    [GeneratedRegex(@"<think\b[^>]*>[\s\S]*?</think\s*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ThinkBlockRegex();

    [GeneratedRegex(@"<thinking\b[^>]*>[\s\S]*?</thinking\s*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ThinkingBlockRegex();

    [GeneratedRegex(@"<reflection\b[^>]*>[\s\S]*?</reflection\s*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ReflectionBlockRegex();

    [GeneratedRegex(@"<\|?redacted_reasoning\|?>[\s\S]*?<\|?/redacted_reasoning\|?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RedactedReasoningBlockRegex();

    [GeneratedRegex(@"<\|?redacted_thinking\|?>[\s\S]*?<\|?/redacted_thinking\|?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex RedactedThinkingBlockRegex();

    [GeneratedRegex(@"<tool_call\b[^>]*>[\s\S]*?</tool_call\s*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ToolCallBlockRegex();

    [GeneratedRegex(@"<function(?:_call)?\b[^>]*>[\s\S]*?</function(?:_call)?\s*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FunctionCallBlockRegex();

    [GeneratedRegex(@"```(?:json|tool|function|xml)?\s*[\r\n]+[\s\S]*?(?:""name""\s*:|""tool""\s*:|""function""\s*:)[\s\S]*?```", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ToolCodeFenceRegex();

    // Bare tool-call JSON objects leaked as plain text (not fenced)
    [GeneratedRegex(@"\{\s*""(?:name|tool|function)""\s*:\s*""[^""]+""\s*,\s*""(?:arguments|parameters|input)""\s*:[\s\S]*?\}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex InlineJsonToolRegex();

    [GeneratedRegex(@"<(?:think|thinking|reflection|tool_call|function(?:_call)?)\b[^>]*>[\s\S]*\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IncompleteThinkOpenRegex();

    [GeneratedRegex(@"<(?:tool_call|function(?:_call)?)\b[^>]*>[\s\S]*\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IncompleteToolOpenRegex();

    // Only hide incomplete tool-ish fences — never generic markdown ``` used in real answers.
    [GeneratedRegex(@"```(?:json|tool|function|xml)\b[\s\S]*\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IncompleteFenceOpenRegex();

    [GeneratedRegex(@"^(?:Thought|Reasoning|Analysis|Plan|Action(?:\s*Input)?|Observation|Function(?:\s*Call)?|Tool(?:\s*Call)?|Inner monologue|Thinking|Scratchpad)\s*:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AgentScratchpadLineRegex();

    [GeneratedRegex(@"<\|[^|>]+?\|>", RegexOptions.CultureInvariant)]
    private static partial Regex SpecialTokenRegex();

    [GeneratedRegex(@"\n{3,}", RegexOptions.CultureInvariant)]
    private static partial Regex CollapseBlankLinesRegex();

    /// <summary>
    /// True when the clerk message likely depends on prior turns (short / deixis / "that fee").
    /// </summary>
    public static bool LooksLikeFollowUp(string current)
    {
        if (string.IsNullOrWhiteSpace(current))
            return false;

        var trimmed = current.Trim();
        if (trimmed.Length <= 48)
            return true;

        var lower = trimmed.ToLowerInvariant();
        return lower.Contains(" that ", StringComparison.Ordinal)
               || lower.StartsWith("that ", StringComparison.Ordinal)
               || lower.Contains(" this ", StringComparison.Ordinal)
               || lower.StartsWith("this ", StringComparison.Ordinal)
               || lower.Contains(" those ", StringComparison.Ordinal)
               || lower.Contains(" these ", StringComparison.Ordinal)
               || lower.Contains(" it ", StringComparison.Ordinal)
               || lower.StartsWith("it ", StringComparison.Ordinal)
               || lower is "it" or "that" or "this"
               || lower.Contains("the fee", StringComparison.Ordinal)
               || lower.Contains("the one", StringComparison.Ordinal)
               || lower.Contains("same one", StringComparison.Ordinal)
               || lower.Contains("above", StringComparison.Ordinal)
               || lower.Contains("previous", StringComparison.Ordinal)
               || lower.StartsWith("and ", StringComparison.Ordinal)
               || lower.StartsWith("what about", StringComparison.Ordinal)
               || lower.StartsWith("how about", StringComparison.Ordinal)
               || lower.StartsWith("who do i", StringComparison.Ordinal)
               || lower.StartsWith("where do i", StringComparison.Ordinal);
    }

    /// <summary>
    /// Query string for semantic search only. Follow-ups prepend recent user turns; chat still uses the clerk's words.
    /// </summary>
    public static string BuildRetrievalQuery(string current, IReadOnlyList<string> recentUserTurns)
    {
        var question = (current ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(question))
            return string.Empty;

        if (!LooksLikeFollowUp(question) || recentUserTurns is not { Count: > 0 })
            return question;

        var prior = recentUserTurns
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .TakeLast(2)
            .Select(t => t.Trim());
        var joined = string.Join(" ", prior);
        return string.IsNullOrWhiteSpace(joined) ? question : $"{joined} {question}";
    }

    /// <summary>
    /// User texts from prior history (plain questions only — not RAG-packed messages).
    /// </summary>
    public static IReadOnlyList<string> GetRecentUserTexts(IReadOnlyList<ChatMessage> history, int take = 2)
    {
        if (history is null || history.Count == 0 || take <= 0)
            return [];

        return history
            .Where(m => m.Role == ChatRole.User)
            .Select(m => m.Text ?? string.Empty)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .TakeLast(take)
            .ToList();
    }

    /// <summary>
    /// Packs System + capped prior turns + current RAG user message. Prior turns must be plain text (no re-injected RAG).
    /// </summary>
    public static List<ChatMessage> BuildChatMessages(
        string systemPrompt,
        IReadOnlyList<ChatMessage> priorHistory,
        string currentRagUserMessage,
        int maxTurns = DefaultMaxHistoryTurns)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt ?? string.Empty)
        };

        foreach (var prior in TakeLastTurns(priorHistory, maxTurns))
            messages.Add(prior);

        messages.Add(new ChatMessage(ChatRole.User, currentRagUserMessage ?? string.Empty));
        return messages;
    }

    /// <summary>
    /// Appends a plain user question + assistant reply and drops oldest pairs beyond <paramref name="maxTurns"/>.
    /// </summary>
    public static void AppendTurn(
        List<ChatMessage> history,
        string plainUserQuestion,
        string assistantReply,
        int maxTurns = DefaultMaxHistoryTurns)
    {
        ArgumentNullException.ThrowIfNull(history);
        history.Add(new ChatMessage(ChatRole.User, plainUserQuestion ?? string.Empty));
        history.Add(new ChatMessage(ChatRole.Assistant, assistantReply ?? string.Empty));
        TrimToMaxTurns(history, maxTurns);
    }

    /// <summary>
    /// Keeps at most <paramref name="maxTurns"/> user+assistant pairs (oldest dropped).
    /// </summary>
    public static void TrimToMaxTurns(List<ChatMessage> history, int maxTurns = DefaultMaxHistoryTurns)
    {
        ArgumentNullException.ThrowIfNull(history);
        if (maxTurns < 1)
        {
            history.Clear();
            return;
        }

        var maxMessages = maxTurns * 2;
        while (history.Count > maxMessages)
            history.RemoveAt(0);
    }

    /// <summary>
    /// True when a prior history message looks like a RAG-packed user block (must not be re-sent as history).
    /// </summary>
    public static bool LooksLikeRagPackedUserMessage(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        return text.Contains("Relevant documents:", StringComparison.Ordinal)
               || text.Contains("Relevant institutional knowledge:", StringComparison.Ordinal)
               || text.Contains("Required Sources to cite", StringComparison.Ordinal)
               || text.Contains("\n\nQuestion: ", StringComparison.Ordinal);
    }

    private static IEnumerable<ChatMessage> TakeLastTurns(IReadOnlyList<ChatMessage>? history, int maxTurns)
    {
        if (history is null || history.Count == 0 || maxTurns < 1)
            yield break;

        var maxMessages = maxTurns * 2;
        var start = Math.Max(0, history.Count - maxMessages);
        for (var i = start; i < history.Count; i++)
            yield return history[i];
    }
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

    public static string ConvertToPdfSuccess(string fileName) =>
        $"Converted {fileName} to PDF — downloaded and saved to the library.";

    public static string ConvertToPdfLibraryOnly(string pdfFileName) =>
        $"Saved \"{pdfFileName}\" to the library. Opening Smart PDF workspace…";

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

    public static string WorkspaceSaveSuccess(string fileName) => $"Saved \"{fileName}\" to NAS.";
    public static string WorkspaceSaveFailed(string? detail = null) =>
        string.IsNullOrWhiteSpace(detail) ? "Could not save document to NAS." : $"Could not save to NAS: {detail}";
    public static string WorkspaceDirtyDiscardPrompt() =>
        "You have unsaved changes. Discard them and close?";
    public static string PdfSaveInProgress() => "Saving PDF annotations to NAS…";
    public static string SoftDeleted(string fileName) => $"Moved \"{fileName}\" to Recycle bin.";
    public static string Restored(string fileName) => $"Restored \"{fileName}\" to the library.";
    public static string Purged(string fileName) => $"Permanently deleted \"{fileName}\".";
    public static string VersionRestored(int versionNumber) => $"Restored version {versionNumber} as current content.";
    public static string AnnotationExportSuccess() => "Downloaded annotation export.";
    public static string AnnotationImportSuccess() => "Imported annotations into the viewer.";

    /// <summary>Simple type glyph for library grid (thumbnail substitute until image thumbs ship).</summary>
    public static string FileTypeIconCss(string? fileName)
    {
        var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
        return ext switch
        {
            ".pdf" => "e-icons e-export-pdf",
            ".doc" or ".docx" => "e-icons e-file-document",
            ".xls" or ".xlsx" or ".csv" => "e-icons e-table",
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".tif" or ".tiff" => "e-icons e-image",
            ".txt" or ".md" => "e-icons e-description",
            _ => "e-icons e-file"
        };
    }
}
