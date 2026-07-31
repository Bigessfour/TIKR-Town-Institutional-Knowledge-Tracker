namespace TIKR.Shared.Helpers;

/// <summary>Limits for persisted assistant chat content (SQLite / prompt safety).</summary>
public static class ChatHistoryLimits
{
    public const int MaxMessageChars = 16_000;
    public const int MaxMemoryFactValueChars = 2_000;
    public const int MaxMemoryFactKeyChars = 100;
    public const int MaxTitleChars = 200;

    /// <summary>HTTP header for local clerk isolation when JWT auth is off.</summary>
    public const string ChatUserHeaderName = "X-Tikr-Chat-User";

    public static string TruncateMessage(string? text) => Truncate(text, MaxMessageChars);

    public static string Truncate(string? text, int maxChars)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;
        var trimmed = text.Trim();
        return trimmed.Length <= maxChars ? trimmed : trimmed[..maxChars];
    }
}
