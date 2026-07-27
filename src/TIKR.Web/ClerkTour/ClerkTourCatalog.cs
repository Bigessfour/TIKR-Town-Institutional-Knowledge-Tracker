namespace TIKR.Web.ClerkTour;

public sealed record ClerkTourStep(
    string Element,
    string Title,
    string Description,
    string? Route = null);

public static class ClerkTourCatalog
{
    public const string CurrentVersion = "v2";

    private static string Sel(string tourId) => $"[data-tour='{tourId}']";

    public static IReadOnlyList<ClerkTourStep> GetGlobalSteps() =>
    [
        new(Sel(ClerkTourIds.NavDashboard), "Dashboard", "Your home screen lists today's priorities from Colorado deadlines and open requirements."),
        new(Sel(ClerkTourIds.NavRequirements), "Requirements", "Add, edit, and export statutory and town-specific deadlines here."),
        new(Sel(ClerkTourIds.NavDocuments), "Documents", "Upload and search ordinances, minutes, and forms stored on the NAS."),
        new(Sel(ClerkTourIds.NavAssistant), "AI Assistant", "Ask questions in plain English; local Ollama keeps routine chat on your NAS."),
        new(Sel(ClerkTourIds.ThemeSelect), "Display theme", "Switch Light, Dark, or High contrast. Saved in this browser only."),
        new(Sel(ClerkTourIds.FooterStatus), "Status footer", "Shows NAS connectivity and whether Ollama is ready for local AI."),
    ];

    public static IReadOnlyList<ClerkTourStep> GetPageSteps(string route) => route switch
    {
        "/" => GetDashboardPageSteps(),
        "/requirements" => GetRequirementsPageSteps(),
        "/calendar" => GetCalendarPageSteps(),
        "/documents" => GetDocumentsPageSteps(),
        "/assistant" => GetAssistantPageSteps(),
        "/vault" => GetVaultPageSteps(),
        "/settings" => GetSettingsPageSteps(),
        _ => [],
    };

    public static IReadOnlyList<ClerkTourStep> GetFullTourSteps()
    {
        var steps = new List<ClerkTourStep>(GetGlobalSteps());
        steps.AddRange(GetDashboardPageSteps());
        steps.AddRange(GetRequirementsPageSteps());
        steps.AddRange(GetCalendarPageSteps());
        steps.AddRange(GetDocumentsPageSteps());
        steps.AddRange(GetAssistantPageSteps());
        steps.AddRange(GetVaultPageSteps());
        steps.AddRange(GetSettingsPageSteps());
        return steps;
    }

    private static IReadOnlyList<ClerkTourStep> GetDashboardPageSteps() =>
    [
        new(Sel(ClerkTourIds.HelpDashboard), "Page help", "Short tips for the screen you are on.", "/"),
        new(Sel(ClerkTourIds.DashboardUserGuide), "User guide", "Open the searchable clerk reference anytime.", "/"),
        new(Sel(ClerkTourIds.DashboardPriorities), "Today's priorities", "Cards show what needs attention; empty state links you to Calendar and Requirements.", "/"),
    ];

    private static IReadOnlyList<ClerkTourStep> GetRequirementsPageSteps() =>
    [
        new(Sel(ClerkTourIds.HelpRequirements), "Page help", "How exports, AI scan, and Colorado seeds work on this page.", "/requirements"),
        new(Sel(ClerkTourIds.ReqAdd), "Add requirement", "Create town-specific deadlines beyond pre-seeded Colorado items.", "/requirements"),
        new(Sel(ClerkTourIds.ReqExportCsv), "Export CSV", "Download the grid for council packets or your own tracking.", "/requirements"),
        new(Sel(ClerkTourIds.ReqFilters), "Search and filters", "Narrow by title, category, urgency, or show completed items.", "/requirements"),
        new(Sel(ClerkTourIds.ReqGrid), "Requirements grid", "Select a row to edit, mark complete, or delete.", "/requirements"),
    ];

    private static IReadOnlyList<ClerkTourStep> GetCalendarPageSteps() =>
    [
        new(Sel(ClerkTourIds.HelpCalendar), "Page help", "Create, edit, move, or delete deadlines here; Colorado defaults cannot be deleted.", "/calendar"),
        new(Sel(ClerkTourIds.CalSchedule), "Deadline calendar", "Month, Week, and Agenda views. Double-click to add, drag to move, or open an event to edit — changes save to Requirements.", "/calendar"),
    ];

    private static IReadOnlyList<ClerkTourStep> GetDocumentsPageSteps() =>
    [
        new(Sel(ClerkTourIds.HelpDocuments), "Page help", "Upload, AI tagging, and NAS storage explained.", "/documents"),
        new(Sel(ClerkTourIds.DocUploader), "Upload documents", "Drag and drop or browse — files stay on your Synology.", "/documents"),
        new(Sel(ClerkTourIds.DocSearch), "Search mode", "Full-text keyword search or semantic (meaning-based) search.", "/documents"),
        new(Sel(ClerkTourIds.DocLibrary), "Folders and grid", "Browse folders, select rows, and preview on the right — PDF Viewer, Word, or Spreadsheet by file type.", "/documents"),
    ];

    private static IReadOnlyList<ClerkTourStep> GetAssistantPageSteps() =>
    [
        new(Sel(ClerkTourIds.HelpAssistant), "Page help", "Local chat vs Advanced AI (Grok) when enabled on the API.", "/assistant"),
        new(Sel(ClerkTourIds.AsstChat), "Chat", "Type a question; answers stream from Ollama on the NAS by default.", "/assistant"),
        new(Sel(ClerkTourIds.AsstAdvanced), "Ask Advanced AI", "Uses the API for harder reasoning after you have sent a chat message.", "/assistant"),
    ];

    private static IReadOnlyList<ClerkTourStep> GetVaultPageSteps() =>
    [
        new(Sel(ClerkTourIds.HelpVault), "Page help", "Succession planning and institutional knowledge.", "/vault"),
        new(Sel(ClerkTourIds.VaultCopy), "Copy for new clerk", "One-click export of vault content for handoff.", "/vault"),
        new(Sel(ClerkTourIds.VaultTabs), "Vault tabs", "How-To, contacts, passwords policy, and voice notes.", "/vault"),
    ];

    private static IReadOnlyList<ClerkTourStep> GetSettingsPageSteps() =>
    [
        new(Sel(ClerkTourIds.HelpSettings), "Page help", "Theme, helper options, and town details — tap Call Steve for help if unsure.", "/settings"),
        new(Sel(ClerkTourIds.TourReplay), "Replay walkthrough", "Run the full product tour again.", "/settings"),
        new(Sel(ClerkTourIds.UserGuideOpen), "User guide", "Searchable task reference for clerks.", "/settings"),
        new(Sel(ClerkTourIds.SettingsDeployment), "Something else?", "Networking and install setup are handled by Steve.", "/settings"),
    ];
}
