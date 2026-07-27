namespace TIKR.Shared.DTOs;

/// <summary>Clerk-editable settings. Secrets are never returned in plaintext.</summary>
public record FeatureSettingsDto(
    string OllamaHost,
    string OllamaChatModel,
    bool UseGrok,
    bool GrokApiKeyConfigured,
    bool OllamaAvailable,
    string? StatusMessage = null,
    string? GrokModel = null,
    bool SyncfusionLicenseKeyConfigured = false,
    string? SyncfusionLicenseHint = null,
    string? GrokApiKeyHint = null,
    string FileStoragePath = "",
    string TownName = "Wiley",
    string StorageLabel = "Synology NAS",
    string? TownLogoPath = null,
    bool OcrEnabled = true,
    bool UseSyncfusionAgentTools = false,
    bool UseSyncfusionAgentOrchestration = false,
    string? LibraryScanPath = null,
    int LibraryScanIntervalSeconds = 300,
    int LibraryScanMaxImports = 500,
    string? EmailInboxPath = null);

public record UpdateFeatureSettingsRequest(
    string? OllamaHost = null,
    string? OllamaChatModel = null,
    bool? UseGrok = null,
    /// <summary>Null = leave unchanged. Empty string = clear clerk-stored key.</summary>
    string? GrokApiKey = null,
    string? GrokModel = null,
    /// <summary>Null = leave unchanged. Empty string = clear clerk-stored key.</summary>
    string? SyncfusionLicenseKey = null,
    string? FileStoragePath = null,
    bool? MoveExistingDocuments = null,
    string? TownName = null,
    string? StorageLabel = null,
    string? TownLogoPath = null,
    bool? OcrEnabled = null,
    bool? UseSyncfusionAgentTools = null,
    bool? UseSyncfusionAgentOrchestration = null,
    string? LibraryScanPath = null,
    int? LibraryScanIntervalSeconds = null,
    int? LibraryScanMaxImports = null,
    string? EmailInboxPath = null);
