using Microsoft.Extensions.Configuration;
using TIKR.Shared.Constants;

namespace TIKR.Shared.Configuration;

public static class TikrConfiguration
{
    public static string GetDatabaseProvider(IConfiguration configuration) =>
        configuration["DATABASE_PROVIDER"] ?? "Sqlite";

    public static string GetFileStoragePath(IConfiguration configuration) =>
        configuration["FileStorage:BasePath"]
        ?? configuration["FILE_STORAGE_PATH"]
        ?? Path.Combine(Directory.GetCurrentDirectory(), "data", "documents");

    public static string GetOllamaHost(IConfiguration configuration) =>
        configuration["OLLAMA_HOST"]
        ?? configuration["AI:OllamaHost"]
        ?? "http://localhost:11434";

    public static string GetChatModel(IConfiguration configuration) =>
        configuration["AI:DefaultModel"]
        ?? configuration["OLLAMA_CHAT_MODEL"]
        ?? "llama3.2:3b";

    public static bool GetUseGrok(IConfiguration configuration)
    {
        if (bool.TryParse(configuration["USE_GROK"], out var fromEnv))
            return fromEnv;

        if (configuration.GetSection("AI").GetValue<bool?>("UseGrok") is { } useGrok)
            return useGrok;

        return false;
    }

    public static string? GetGrokApiKey(IConfiguration configuration) =>
        configuration["GROK_API_KEY"] ?? configuration["XAI_API_KEY"];

    public static string GetGrokModel(IConfiguration configuration) =>
        configuration["AI:GrokModel"]
        ?? configuration["GROK_MODEL"]
        ?? "grok-4.3";

    public static bool IsAuthEnabled(IConfiguration configuration)
    {
        if (bool.TryParse(configuration["TIKR_AUTH_ENABLED"], out var explicitEnabled))
            return explicitEnabled;

        var email = GetAdminBootstrapEmail(configuration);
        var password = GetAdminBootstrapPassword(configuration);
        return !string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password);
    }

    public static string? GetAdminBootstrapEmail(IConfiguration configuration) =>
        configuration["TIKR_ADMIN_EMAIL"];

    public static string? GetAdminBootstrapPassword(IConfiguration configuration) =>
        configuration["TIKR_ADMIN_PASSWORD"];

    public static string GetJwtSigningKey(IConfiguration configuration)
    {
        var key = configuration["TIKR_JWT_SIGNING_KEY"];
        if (!string.IsNullOrWhiteSpace(key))
            return key;

        if (IsAuthEnabled(configuration))
            throw new InvalidOperationException(
                "TIKR_JWT_SIGNING_KEY is required when authentication is enabled.");

        return TikrAuthDefaults.DevDisabledJwtSigningKey;
    }

    public static int GetJwtExpirationHours(IConfiguration configuration) =>
        int.TryParse(configuration["TIKR_JWT_EXPIRATION_HOURS"], out var hours) && hours > 0
            ? hours
            : 8;

    public static bool GetUseSyncfusionAgentTools(IConfiguration configuration) =>
        bool.TryParse(configuration["USE_SYNCFUSION_AGENT_TOOLS"], out var enabled) && enabled;

    /// <summary>
    /// When true (and agent tools enabled), Ollama orchestrates Syncfusion tool calls via Microsoft.Extensions.AI.
    /// Requires Ollama at <see cref="GetOllamaHost"/> with a tool-capable chat model.
    /// </summary>
    public static bool GetUseSyncfusionAgentOrchestration(IConfiguration configuration) =>
        GetUseSyncfusionAgentTools(configuration) &&
        bool.TryParse(configuration["USE_SYNCFUSION_AGENT_ORCHESTRATION"], out var enabled) && enabled;

    public static string? GetAgentStorageKey(IConfiguration configuration) =>
        configuration["TIKR_AGENT_STORAGE_KEY"];

    /// <summary>
    /// Local folder for forward-to-folder / IMAP drop ingestion. When set, a background poller ingests files into Documents.
    /// </summary>
    public static string? GetEmailInboxPath(IConfiguration configuration)
    {
        var path = configuration["TIKR_EMAIL_INBOX_PATH"];
        return string.IsNullOrWhiteSpace(path) ? null : path.Trim();
    }

    /// <summary>
    /// Root folder of an existing NAS document library to scan (copy into TIKR + tag/embed).
    /// Bind-mount this path into the API container. Source files are never moved or deleted.
    /// </summary>
    public static string? GetLibraryScanPath(IConfiguration configuration)
    {
        var path = configuration["TIKR_LIBRARY_SCAN_PATH"];
        return string.IsNullOrWhiteSpace(path) ? null : path.Trim();
    }

    /// <summary>Background poll interval for NAS library scan. Default 300 seconds.</summary>
    public static int GetLibraryScanIntervalSeconds(IConfiguration configuration) =>
        int.TryParse(configuration["TIKR_LIBRARY_SCAN_INTERVAL_SECONDS"], out var seconds) && seconds > 0
            ? seconds
            : 300;

    /// <summary>
    /// Max new imports per library scan pass. Default 500 (accuracy-first bulk corpus; resume via fingerprints).
    /// Set <c>TIKR_LIBRARY_SCAN_MAX_IMPORTS=0</c> for unlimited per run.
    /// </summary>
    public static int GetLibraryScanMaxImportsPerRun(IConfiguration configuration)
    {
        if (!int.TryParse(configuration["TIKR_LIBRARY_SCAN_MAX_IMPORTS"], out var max) || max < 0)
            return 500;
        return max == 0 ? int.MaxValue : max;
    }

    /// <summary>
    /// When true, forgot-password responses include the reset token (local/dev without SMTP).
    /// Defaults to true in Development; otherwise require explicit env flag.
    /// </summary>
    public static bool ExposePasswordResetToken(IConfiguration configuration, bool isDevelopment)
    {
        if (bool.TryParse(configuration["TIKR_AUTH_EXPOSE_RESET_TOKEN"], out var explicitFlag))
            return explicitFlag;
        return isDevelopment;
    }

    public static int GetJwtRefreshExpirationDays(IConfiguration configuration) =>
        int.TryParse(configuration["TIKR_JWT_REFRESH_DAYS"], out var days) && days > 0
            ? days
            : 14;

    /// <summary>
    /// Runtime Syncfusion license key (Blazor UI + Document SDK). Set via docker/.env, user-secrets, or CI secret — never commit.
    /// </summary>
    public static string? GetSyncfusionLicenseKey(IConfiguration configuration)
    {
        var key = configuration["SYNCFUSION_LICENSE_KEY"];
        return string.IsNullOrWhiteSpace(key) ? null : key.Trim();
    }

    /// <summary>
    /// When true (default), sparse PDF/Word extractions are enriched via Syncfusion PDF OCR (Tesseract).
    /// Disable with <c>TIKR_OCR_ENABLED=false</c> if OCR natives are unavailable.
    /// </summary>
    public static bool GetOcrEnabled(IConfiguration configuration)
    {
        if (bool.TryParse(configuration["TIKR_OCR_ENABLED"], out var enabled))
            return enabled;
        return true;
    }

    /// <summary>Optional override for Tesseract language data folder (eng.traineddata).</summary>
    public static string? GetTessDataPath(IConfiguration configuration)
    {
        var path = configuration["TIKR_TESSADATA_PATH"];
        return string.IsNullOrWhiteSpace(path) ? null : path.Trim();
    }
}
