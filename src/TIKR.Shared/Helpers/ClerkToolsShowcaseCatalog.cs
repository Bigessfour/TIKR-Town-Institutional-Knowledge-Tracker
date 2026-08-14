namespace TIKR.Shared.Helpers;

/// <summary>One clerk-facing demo act: product job + Syncfusion surface + how to try it.</summary>
public sealed record ClerkToolsShowcaseAct(
    int Order,
    string Id,
    string Title,
    string ClerkJob,
    string Route,
    string SyncfusionPackages,
    string SyncfusionProcess,
    IReadOnlyList<string> TrySteps,
    string? FixtureHint = null);

/// <summary>
/// Ordered showcase of clerk tools as they appear in TIKR — not a Syncfusion sample browser.
/// Aligns with Syncfusion's document lifecycle (organize → open → work → save) and AI AssistView flows.
/// </summary>
public static class ClerkToolsShowcaseCatalog
{
    public const string PageRoute = "/demo/clerk-tools";

    /// <summary>
    /// Syncfusion's suggested learning/demo path (docs + Sample Browser), mapped for presenters.
    /// </summary>
    public static IReadOnlyList<string> SyncfusionSuggestedProcess { get; } =
    [
        "Sample Browser / live demos — explore default functionalities per control (PDF Viewer feature tour, File Manager, Document Editor, Spreadsheet).",
        "Getting-started samples on GitHub — Blazor Server Interactive: register services, themes CSS, package scripts before blazor.web.js.",
        "Document lifecycle — Organize (File Manager) → Open → Work (annotate / edit / form fill / AssistView) → Save back to storage.",
        "PDF Viewer path — Open/Load → annotations → AcroForm fill → toolbar customize; Smart PDF adds AssistView (and optional Smart Redact / Fill) via IChatClient.",
        "Smart components — AddChatClient (Ollama) + AddSyncfusionSmartComponents().InjectOpenAIInference() then enable AssistView / Smart Paste / Smart TextArea.",
        "Blazor Playground — quick interactive experiments outside the product (optional for IT, not for Deb's daily path).",
    ];

    public static IReadOnlyList<ClerkToolsShowcaseAct> Acts { get; } =
    [
        new(
            1,
            "dashboard",
            "Dashboard priorities",
            "See what is due today without opening binders.",
            "/",
            "SfDashboardLayout, SfGrid, SfButton",
            "Layout + data grids — Syncfusion Sample Browser Dashboard Layout / Grid overview.",
            [
                "Open Dashboard from the sidebar.",
                "Point at today's priority cards and the upcoming deadlines grid.",
                "Use a quick action to jump to Requirements or Calendar.",
            ]),

        new(
            2,
            "requirements",
            "Requirements & playbook checklists",
            "Track Colorado deadlines and election playbook steps; AI helps pre-fill.",
            "/requirements",
            "SfGrid, SfDataForm, SfDialog, SfSmartPasteButton, SfSmartTextArea, SfUploader",
            "Smart Paste / Smart TextArea — Syncfusion Ollama path: IChatClient + InjectOpenAIInference.",
            [
                "Open Requirements → filter or search for Election Canvass.",
                "Edit → show Playbook checklist → mark one step complete.",
                "In Add/Edit, try Smart Paste from clipboard or Smart TextArea suggestions (Ollama must be Connected).",
                "Optional: AI Scan uploaded doc → review dialog → Save.",
            ],
            "tests/fixtures/agent-scan/minimal-clerk-report.pdf or wiley-periodic-report.txt"),

        new(
            3,
            "calendar",
            "Deadline calendar",
            "See due-outs on a month/week agenda clerks already understand.",
            "/calendar",
            "SfSchedule",
            "Scheduler demos — create / edit / drag; TIKR maps events to Requirements.",
            [
                "Open Calendar.",
                "Switch Month / Week / Agenda.",
                "Open an event (e.g. Election Canvass) and note playbook progress in the subject.",
            ]),

        new(
            4,
            "documents-library",
            "Document library & File Manager",
            "Upload mail, search packets, browse folders on the NAS.",
            "/documents",
            "SfUploader, SfTreeView, SfGrid, SfFileManager, SfSplitter, SfContextMenu",
            "File Manager — Syncfusion organize step: browse → rename/move → OnFileOpen into workspace.",
            [
                "Library mode: upload a small file; select a row; show preview pane.",
                "Toggle semantic search → query previous election canvass.",
                "Browse mode: show Syncfusion File Manager folders; double-click a file to open workspace.",
            ],
            "Any small PDF/TXT; prefer tests/fixtures/agent-scan/minimal-clerk-report.pdf"),

        new(
            5,
            "smart-pdf",
            "Smart PDF workspace",
            "Annotate, ask the PDF questions, then Save to NAS.",
            "/documents",
            "SfSmartPdfViewer, Syncfusion.Blazor.AI (AssistView via IChatInferenceService)",
            "PDF Viewer open/save/annotate (+ Smart AssistView). Syncfusion: Load → work → save; AssistView Enable=true with StreamResponse.",
            [
                "Open a PDF → Full Screen (or double-click from Browse).",
                "Show toolbar: annotate / highlight.",
                "Open AssistView (Ask about this PDF) → ask for a clerk summary (Ollama Connected).",
                "Save to NAS → wait for toast.",
            ],
            "Demo PDF in library; Smart Redact/Fill stay off until you enable them for a separate act."),

        new(
            6,
            "word-spreadsheet",
            "Word & Spreadsheet editors",
            "Edit trustee letters and spreadsheets without leaving TIKR.",
            "/documents",
            "SfDocumentEditorContainer (WordProcessor), SfSpreadsheet",
            "Document Editor / Spreadsheet getting-started — open workbook or SFDT → edit → save.",
            [
                "Open a .docx → Full Screen → edit a sentence → Save to NAS.",
                "Open a .xlsx → show ribbon / formula bar → Save to NAS.",
            ],
            "Seed or upload a sample .docx and .xlsx before the demo."),

        new(
            7,
            "assistant",
            "AI Assistant (local-first)",
            "Ask plain-English questions; answers stay on the NAS unless Advanced AI is on.",
            "/assistant",
            "SfAIAssistView (InteractiveChat), Cards, Buttons",
            "AI AssistView — PromptRequested + streaming/buffered responses; TIKR uses IChatClient directly for RAG.",
            [
                "Ask: What should I prioritize this week for Wiley?",
                "Show suggestion chips and Clear conversation.",
                "Mention Advanced AI only if Grok is staged for Act 2.",
            ]),

        new(
            8,
            "vault",
            "Knowledge Vault",
            "Contacts, how-tos, and voice notes the next clerk needs on day one.",
            "/vault",
            "SfGrid, SfTab, SfAccordion, SfRichTextEditor, SfSpeechToText, SfSmartTextArea",
            "RTE + Speech + Smart TextArea samples; Smart TextArea shares Ollama IChatInferenceService.",
            [
                "Contacts tab → Add contact (Election) → Save.",
                "Optional: How-To entry or short voice note.",
                "Show Smart TextArea assist while typing a contact note.",
            ]),

        new(
            9,
            "settings-themes",
            "Settings, themes & status",
            "Confirm local AI, license, storage label, and display theme.",
            "/settings",
            "SfCard, SfDropDownList, Themes (bootstrap5 / dark / high-contrast)",
            "Themes package + dynamic CSS swap — Sample Browser theming.",
            [
                "Show Ollama Connected and Syncfusion license status.",
                "Toggle Light / Dark / High contrast from the sidebar.",
                "Open this Tools tour again from Settings if needed.",
            ]),
    ];

    public static ClerkToolsShowcaseAct? GetById(string? id) =>
        string.IsNullOrWhiteSpace(id)
            ? null
            : Acts.FirstOrDefault(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase));
}
