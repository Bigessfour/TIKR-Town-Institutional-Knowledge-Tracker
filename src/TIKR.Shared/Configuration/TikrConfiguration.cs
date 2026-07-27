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

    public static string GetOllamaHost(IConfiguration configuration)
    {
        var host = configuration["OLLAMA_HOST"]
            ?? configuration["AI:OllamaHost"]
            ?? "http://localhost:11434";
        return RewriteDockerOnlyOllamaHost(host);
    }

    /// <summary>
    /// docker/.env often sets <c>OLLAMA_HOST=http://ollama:11434</c> for Compose.
    /// On host <c>dotnet run</c> that hostname does not resolve — rewrite to loopback.
    /// Inside a container, leave the Docker service name alone.
    /// </summary>
    public static string RewriteDockerOnlyOllamaHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host) || !LooksLikeDockerOllamaHostname(host))
            return host;

        if (IsRunningInsideContainer())
            return host;

        return "http://127.0.0.1:11434";
    }

    public static bool LooksLikeDockerOllamaHostname(string host)
    {
        if (!Uri.TryCreate(host, UriKind.Absolute, out var uri))
            return false;

        return uri.Host.Equals("ollama", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Equals("tikr-ollama", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsRunningInsideContainer() =>
        File.Exists("/.dockerenv")
        || string.Equals(
            Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
            "true",
            StringComparison.OrdinalIgnoreCase);

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

    /// <summary>
    /// Default xAI chat model for Advanced AI. Prefer <c>grok-4.5</c>
    /// (see https://docs.x.ai/docs/models — recommended for chat/code agents).
    /// </summary>
    public const string DefaultGrokModel = "grok-4.5";

    /// <summary>OpenAI-compatible chat completions base (no trailing slash).</summary>
    public const string GrokApiBaseUrl = "https://api.x.ai/v1";

    public static string GetGrokModel(IConfiguration configuration)
    {
        var configured = configuration["AI:GrokModel"]
                         ?? configuration["GROK_MODEL"]
                         ?? DefaultGrokModel;
        return NormalizeGrokModel(configured);
    }

    /// <summary>
    /// Map empty / retired defaults to the current xAI chat default without
    /// rewriting intentional pins (e.g. a dated model id a maintainer set).
    /// </summary>
    public static string NormalizeGrokModel(string? model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return DefaultGrokModel;

        var m = model.Trim();
        // Pre-4.5 TIKR default; upgrade so Advanced AI tracks xAI's current recommendation.
        if (string.Equals(m, "grok-4.3", StringComparison.OrdinalIgnoreCase)
            || string.Equals(m, "grok-3", StringComparison.OrdinalIgnoreCase)
            || string.Equals(m, "grok-2-latest", StringComparison.OrdinalIgnoreCase)
            || string.Equals(m, "grok-beta", StringComparison.OrdinalIgnoreCase))
            return DefaultGrokModel;

        return m;
    }

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
    /// <summary>
    /// How often the embedding recovery host checks Ollama + corpus coverage (seconds). Default 60.
    /// Set <c>TIKR_EMBEDDING_RECOVERY_INTERVAL_SECONDS=0</c> to disable auto-recovery.
    /// </summary>
    public static int GetEmbeddingRecoveryIntervalSeconds(IConfiguration configuration)
    {
        if (int.TryParse(configuration["TIKR_EMBEDDING_RECOVERY_INTERVAL_SECONDS"], out var fromEnv))
            return Math.Max(0, fromEnv);
        return 60;
    }

    /// <summary>
    /// Minimum minutes between automatic reindex runs (prevents thrashing). Default 15.
    /// </summary>
    public static int GetEmbeddingRecoveryCooldownMinutes(IConfiguration configuration)
    {
        if (int.TryParse(configuration["TIKR_EMBEDDING_RECOVERY_COOLDOWN_MINUTES"], out var fromEnv))
            return Math.Max(1, fromEnv);
        return 15;
    }

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
