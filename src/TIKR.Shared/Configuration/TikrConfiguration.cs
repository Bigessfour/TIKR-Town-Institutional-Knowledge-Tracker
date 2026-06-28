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
        configuration["AI:OllamaHost"]
        ?? configuration["OLLAMA_HOST"]
        ?? "http://localhost:11434";

    public static string GetChatModel(IConfiguration configuration) =>
        configuration["AI:DefaultModel"]
        ?? configuration["OLLAMA_CHAT_MODEL"]
        ?? "llama3.2:3b";

    public static bool GetUseGrok(IConfiguration configuration)
    {
        if (configuration.GetSection("AI").GetValue<bool?>("UseGrok") is { } useGrok)
            return useGrok;

        return bool.TryParse(configuration["USE_GROK"], out var enabled) && enabled;
    }

    public static string? GetGrokApiKey(IConfiguration configuration) =>
        configuration["GROK_API_KEY"];

    public static string GetGrokModel(IConfiguration configuration) =>
        configuration["AI:GrokModel"]
        ?? configuration["GROK_MODEL"]
        ?? "grok-2-latest";

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
}
