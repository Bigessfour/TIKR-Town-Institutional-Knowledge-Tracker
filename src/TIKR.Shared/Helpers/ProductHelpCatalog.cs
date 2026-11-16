namespace TIKR.Shared.Helpers;

/// <summary>One TIKR product / Syncfusion workspace help article for Assistant product RAG.</summary>
public sealed record ProductHelpEntry(
    string Id,
    string Title,
    string Body,
    string? RouteHint,
    IReadOnlyList<string> Keywords);

/// <summary>
/// Curated operations knowledge for Deb/Paige: how TIKR works and Syncfusion document tools.
/// Keyword-ranked search — no extra Ollama call (packs into the chat turn with town RAG).
/// </summary>
public static class ProductHelpCatalog
{
    public static IReadOnlyList<ProductHelpEntry> All { get; } = BuildEntries();

    public static IReadOnlyList<ProductHelpEntry> Search(string? query, int topK = 3)
    {
        topK = Math.Clamp(topK, 1, 8);
        if (string.IsNullOrWhiteSpace(query))
            return All.Take(topK).ToList();

        var tokens = Tokenize(query);
        if (tokens.Count == 0)
            return All.Take(topK).ToList();

        return All
            .Select(e => (Entry: e, Score: Score(e, tokens, query)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => x.Entry)
            .ToList();
    }

    public static string FormatForPrompt(IReadOnlyList<ProductHelpEntry> hits)
    {
        if (hits.Count == 0)
            return string.Empty;

        var lines = hits.Select(h =>
        {
            var route = string.IsNullOrWhiteSpace(h.RouteHint) ? "" : $" (open: {h.RouteHint})";
            return $"- [{h.Title}]{route}\n  {h.Body.Trim()}";
        });
        return "TIKR product help (how the app works — prefer this for how-to / Syncfusion questions):\n" +
               string.Join("\n\n", lines);
    }

    public static IReadOnlyList<string> DefaultSuggestionChips { get; } =
    [
        "What should I work on this week?",
        "How do I open a document full screen?",
        "How do I save PDF changes?",
        "How do I link a packet to a due-out?",
        "What is Smart Redact?",
        "How do I upload today's mail?",
    ];

    private static int Score(ProductHelpEntry e, HashSet<string> tokens, string rawQuery)
    {
        var score = 0;
        var title = e.Title.ToLowerInvariant();
        var body = e.Body.ToLowerInvariant();
        var raw = rawQuery.ToLowerInvariant();

        foreach (var t in tokens)
        {
            if (title.Contains(t, StringComparison.Ordinal))
                score += 5;
            if (e.Keywords.Any(k => k.Contains(t, StringComparison.OrdinalIgnoreCase)
                                    || t.Contains(k, StringComparison.OrdinalIgnoreCase)))
                score += 4;
            if (body.Contains(t, StringComparison.Ordinal))
                score += 1;
        }

        if (title.Contains(raw, StringComparison.Ordinal) || raw.Contains(title, StringComparison.Ordinal))
            score += 8;

        return score;
    }

    private static HashSet<string> Tokenize(string query) =>
        query.ToLowerInvariant()
            .Split([' ', '\t', '\n', '\r', ',', '.', '?', '!', ';', ':', '/', '\\', '-', '_'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(t => t.Length > 2)
            .ToHashSet(StringComparer.Ordinal);

    private static List<ProductHelpEntry> BuildEntries() =>
    [
        new(
            "clerk-tools-tour",
            "Clerk tools tour",
            "Maintainers open /demo/clerk-tools directly. It is a presenter walkthrough of the clerk screens. It is not in the clerk menu.",
            "/demo/clerk-tools",
            ["demo", "tour", "showcase", "syncfusion", "tools", "walkthrough", "clerk"]),

        new(
            "chat-memory",
            "Chat memory for Deb Dillon and Paige Lindo",
            "Chat history and remembered facts follow the town sign-in when you are signed in. Otherwise they follow this computer’s usual clerk. See the chat banner on Dashboard, Settings, and Assistant.",
            "/settings",
            ["memory", "history", "deb", "paige", "dillon", "lindo", "who", "clerk", "identity"]),

        new(
            "assistant-basics",
            "Using AI Assistant",
            "Open AI Assistant from the sidebar. Ask in plain English. Everyday answers use local Ollama on the NAS with your documents and vault. Use suggestion chips for common questions. Clear conversation starts a new thread but keeps remembered facts (birthday, call me, remember that…). Ask Advanced AI (Grok) only when enabled for harder reasoning.",
            "/assistant",
            ["assistant", "chat", "ollama", "ask", "grok", "clear", "conversation"]),

        new(
            "dashboard",
            "Dashboard priorities",
            "Dashboard shows what is due, overdue, and priority bands from Requirements. Use it each morning. Open Requirements to edit due-outs or link packets. Chat memory banner shows who this computer is for.",
            "/",
            ["dashboard", "priority", "due", "overdue", "home"]),

        new(
            "requirements",
            "Requirements and due-outs",
            "Requirements Manager tracks Colorado statutory deadlines and custom town due-outs. Add requirement, set Submit to and contacts, mark complete when filed. Link documents as the packet. AI Scan can pre-fill fields from an uploaded document.",
            "/requirements",
            ["requirement", "deadline", "due-out", "packet", "scan", "submit", "statutory"]),

        new(
            "link-packet",
            "Link a packet document to a due-out",
            "Open Requirements → edit the due-out → attach/link a document from the library. The dashboard and assistant can then see linked packet counts. If nothing is linked, treat it as a missing packet.",
            "/requirements",
            ["link", "packet", "attach", "document", "due-out", "missing"]),

        new(
            "documents-upload",
            "Upload today's mail",
            "Open Document Library. Use Add files (or drag and drop). Files save to town storage. AI can suggest tags and folders after upload. Keep for Assistant context unless the filing is one-time/transitory.",
            "/documents",
            ["upload", "mail", "document", "library", "drag", "drop", "tag"]),

        new(
            "documents-fullscreen",
            "Open document workspace to edit",
            "In Document Library, select a document, then Open & edit (or double-click). This opens the Syncfusion document workspace for PDF (Smart PDF), Word, or Spreadsheet. Home dashboard can open linked docs in a lighter workspace.",
            "/documents",
            ["edit", "workspace", "open", "pdf", "word", "excel", "spreadsheet", "full", "screen"]),

        new(
            "save-to-nas",
            "Save PDF or Office edits and refresh Assistant",
            "In the document workspace, make annotations or edits, then click Save. Wait for the success toast — the file is written to the library and Assistant context is refreshed. Closing with unsaved changes asks you to confirm discard. Do not use browser refresh mid-edit.",
            "/documents",
            ["save", "nas", "persist", "annotation", "edit", "unsaved", "assistant", "rag"]),

        new(
            "smart-redact",
            "Smart Redact (PDF)",
            "Open a PDF Full Screen with extended tools (Document Library). Smart Redact is enabled on the Smart PDF viewer for patterns like names, emails, phones. Review redactions carefully, then Save to NAS. Redaction is permanent after save—keep a copy if needed.",
            "/documents",
            ["redact", "smart", "privacy", "pii", "blackout", "ssn"]),

        new(
            "smart-fill",
            "Smart Fill and form fields (PDF)",
            "Full Screen Smart PDF supports form fields and Smart Fill when extended tools are on. Fill interactive fields, then Save to NAS. If fields do not appear, the PDF may not contain AcroForm fields—use annotations instead.",
            "/documents",
            ["fill", "form", "smart", "fields", "acroform"]),

        new(
            "convert-pdf",
            "Convert Word, Excel, or images to PDF",
            "In Document Library (or Full Screen File tools), use Convert to PDF & open when the file type allows. TIKR converts, can save the PDF to the library, and opens Smart PDF for annotate/redact. Original remains unless you delete it.",
            "/documents",
            ["convert", "pdf", "word", "excel", "image", "docx", "xlsx"]),

        new(
            "annotations-export",
            "Export or import PDF annotations",
            "In Full Screen PDF tools, use Annotations → Export JSON or Export XFDF to download markups. Import accepts .json or .xfdf. Import marks the workspace dirty—Save to NAS to keep changes.",
            "/documents",
            ["annotation", "export", "import", "xfdf", "json", "markup"]),

        new(
            "retag-ai",
            "Re-tag a document with AI",
            "Select a document → Re-tag with AI (or bulk re-tag). Ollama suggests tags and folder. Accept or dismiss. Needs Ollama running. Heuristics fill gaps when the model is thin.",
            "/documents",
            ["retag", "tag", "folder", "ai", "classify"]),

        new(
            "document-intake-scan",
            "Scan a document for deadlines and contacts",
            "After upload (or More actions → Scan for deadlines), TIKR builds a confirm brief of due dates, contacts, and action items. Add to Calendar writes Requirements (shown on Calendar); Save contacts updates Vault. Scans run on the NAS by default. Settings → Use advanced AI for document deadline scans (off by default) may send a redacted text excerpt only — never the raw PDF.",
            "/documents",
            ["intake", "deadline", "scan", "due", "contact", "calendar", "grok", "redacted"]),

        new(
            "vault",
            "Knowledge Vault",
            "Vault stores how the town really works: procedures, contacts, voice notes, bus-factor knowledge. Add entries for tribal knowledge. Assistant can retrieve vault passages in chat. Use categories to filter.",
            "/vault",
            ["vault", "knowledge", "tribal", "procedure", "voice", "bus"]),

        new(
            "calendar",
            "Calendar",
            "Calendar shows requirements on a schedule. Add from plain English with Ollama when available. Good for board and filing timelines.",
            "/calendar",
            ["calendar", "schedule", "meeting", "board"]),

        new(
            "settings-ollama",
            "Settings: local AI (Ollama)",
            "Settings shows whether the town helper (Ollama) is ready. You can adjust Ollama address and chat model. Everyday Assistant chat needs Ollama + embeddings model nomic-embed-text for document search.",
            "/settings",
            ["settings", "ollama", "model", "offline", "helper"]),

        new(
            "settings-license",
            "Syncfusion license",
            "Full document editors need a Syncfusion Document SDK license configured (Settings / environment). Without it, Full Screen may show a license message and fall back to Document Library guidance.",
            "/settings",
            ["license", "syncfusion", "sdk", "key"]),

        new(
            "extract-vault",
            "Extract text from a document to Vault",
            "In Full Screen extended tools: Extract text to Vault. TIKR pulls text/tables and creates a Knowledge Vault entry you can edit later.",
            "/documents",
            ["extract", "vault", "text", "tables", "ocr"]),

        new(
            "call-steve",
            "Call Steve for help",
            "When the app is stuck, the license is missing, or a folder looks wrong, the note at the top of Settings says what to tell Steve. This app does not keep a phone number.",
            "/settings",
            ["steve", "help", "support", "stuck", "error"]),
    ];
}
