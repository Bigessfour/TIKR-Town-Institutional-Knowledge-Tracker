namespace TIKR.Shared.Helpers;

/// <summary>Chat user id for a ClerkSuite Cognito subject. Never an email address.</summary>
public static class CognitoChatIdentity
{
    public const string Prefix = "cognito:";

    public static string Format(Guid sub) => Prefix + sub.ToString("D");

    /// <summary>Accepts <c>cognito:{guid}</c>. Rejects emails and other strings.</summary>
    public static bool TryParse(string? raw, out string userId)
    {
        userId = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var value = raw.Trim();
        if (value.Contains('@'))
            return false;

        if (!value.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var sub = value[Prefix.Length..].Trim();
        if (!Guid.TryParse(sub, out var id))
            return false;

        userId = Format(id);
        return true;
    }
}
