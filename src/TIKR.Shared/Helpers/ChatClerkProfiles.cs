namespace TIKR.Shared.Helpers;

/// <summary>
/// Town clerk identities for chat history + durable memory (auth-off trusted PCs).
/// Primary resolution: Windows computer name from NAS Active Backup inventory
/// (<c>Deb_Computer_Backup/DESKTOP-KN6INHL</c>, <c>Paige_Computer_Backup/DESKTOP-O9TCKP1</c>).
/// User ids are stored as <c>local:{key}</c>.
/// </summary>
public static class ChatClerkProfiles
{
    public const string Deb = "deb";
    public const string Paige = "paige";

    /// <summary>Browser key for rare manual override only (not the primary switcher).</summary>
    public const string BrowserOverrideStorageKey = "tikr-chat-clerk-override";

    /// <summary>Config / env: <c>TIKR_CLERK_PROFILE</c> or <c>Tikr:ClerkProfile</c> = deb|paige.</summary>
    public const string ConfigKey = "Tikr:ClerkProfile";
    public const string EnvVarName = "TIKR_CLERK_PROFILE";

    public static IReadOnlyList<(string Key, string Label)> All { get; } =
    [
        (Deb, "Deb Dillon"),
        (Paige, "Paige Lindo"),
    ];

    /// <summary>
    /// Windows computer names → clerk, from Synology shares on mr-storage
    /// (<c>Deb_Computer_Backup</c> / <c>Paige_Computer_Backup</c>).
    /// </summary>
    public static IReadOnlyDictionary<string, string> MachineNameMap { get; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["DESKTOP-KN6INHL"] = Deb,   // Deb_Computer_Backup
            ["DESKTOP-O9TCKP1"] = Paige, // Paige_Computer_Backup
        };

    public static string DisplayName(string? key) =>
        TryNormalize(key, out var normalized)
            ? All.First(p => p.Key == normalized).Label
            : "Unknown clerk";

    /// <summary>
    /// Accepts <c>deb</c>, <c>Deb Dillon</c>, <c>local:paige</c>, etc.
    /// </summary>
    public static bool TryNormalize(string? raw, out string key)
    {
        key = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var value = raw.Trim();
        if (value.StartsWith("local:", StringComparison.OrdinalIgnoreCase))
            value = value["local:".Length..].Trim();

        if (value.Equals("Deb Dillon", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Deb", StringComparison.OrdinalIgnoreCase))
        {
            key = Deb;
            return true;
        }

        if (value.Equals("Paige Lindo", StringComparison.OrdinalIgnoreCase)
            || value.Equals("Paige", StringComparison.OrdinalIgnoreCase))
        {
            key = Paige;
            return true;
        }

        value = value.ToLowerInvariant();
        if (value is Deb or Paige)
        {
            key = value;
            return true;
        }

        return false;
    }

    /// <summary>Map Windows <c>COMPUTERNAME</c> (optionally FQDN) to clerk key.</summary>
    public static bool TryResolveFromMachineName(string? machineName, out string key)
    {
        key = string.Empty;
        if (string.IsNullOrWhiteSpace(machineName))
            return false;

        var host = machineName.Trim();
        var dot = host.IndexOf('.');
        if (dot > 0)
            host = host[..dot];

        return MachineNameMap.TryGetValue(host, out key!);
    }

    public static string ToUserId(string key) =>
        TryNormalize(key, out var normalized)
            ? $"local:{normalized}"
            : throw new ArgumentException("Unknown clerk profile.", nameof(key));
}
