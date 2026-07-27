namespace TIKR.Infrastructure.Services;

/// <summary>In-memory snapshot of clerk-editable settings (env/appsettings + DB overrides).</summary>
public sealed class FeatureSettingsSnapshot
{
    public required string OllamaHost { get; init; }
    public required string OllamaChatModel { get; init; }
    public required bool UseGrok { get; init; }
    public string? GrokApiKey { get; init; }
    public string GrokModel { get; init; } = TIKR.Shared.Configuration.TikrConfiguration.DefaultGrokModel;
    public string? SyncfusionLicenseKey { get; init; }
    public required string FileStoragePath { get; init; }
    public string TownName { get; init; } = "Wiley";
    public string StorageLabel { get; init; } = "Synology NAS";
    public string? TownLogoPath { get; init; }
    public bool OcrEnabled { get; init; } = true;
    public bool UseSyncfusionAgentTools { get; init; }
    public bool UseSyncfusionAgentOrchestration { get; init; }
    public string? LibraryScanPath { get; init; }
    public int LibraryScanIntervalSeconds { get; init; } = 300;
    public int LibraryScanMaxImports { get; init; } = 500;
    public string? EmailInboxPath { get; init; }

    public bool GrokApiKeyConfigured => !string.IsNullOrWhiteSpace(GrokApiKey);
    public bool GrokEnabled => UseGrok && GrokApiKeyConfigured;
    public bool SyncfusionLicenseKeyConfigured => !string.IsNullOrWhiteSpace(SyncfusionLicenseKey);
}

/// <summary>Process-wide cache so services pick up Settings changes without restart.</summary>
public sealed class FeatureSettingsState
{
    private FeatureSettingsSnapshot _current = new()
    {
        OllamaHost = "http://localhost:11434",
        OllamaChatModel = "llama3.2:3b",
        UseGrok = false,
        FileStoragePath = Path.Combine(Directory.GetCurrentDirectory(), "data", "documents")
    };

    public FeatureSettingsSnapshot Current => Volatile.Read(ref _current)!;

    public void Replace(FeatureSettingsSnapshot snapshot) =>
        Volatile.Write(ref _current, snapshot ?? throw new ArgumentNullException(nameof(snapshot)));
}
