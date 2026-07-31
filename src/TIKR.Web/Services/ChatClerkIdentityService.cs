using Microsoft.Extensions.Configuration;
using TIKR.Shared.Helpers;

namespace TIKR.Web.Services;

/// <summary>
/// Circuit-scoped clerk identity for chat history + memory when JWT auth is off.
/// Locked model: this Windows machine is Deb Dillon or Paige Lindo (NAS backup inventory).
/// Optional install env / rare Settings override for unmapped hosts and demos.
/// </summary>
public sealed class ChatClerkIdentityService(IConfiguration configuration)
{
    private bool _initialized;

    /// <summary>Normalized profile key: <c>deb</c> or <c>paige</c>, or empty if unresolved.</summary>
    public string ActiveProfileKey { get; private set; } = string.Empty;

    public string DisplayName =>
        string.IsNullOrEmpty(ActiveProfileKey)
            ? "Unknown clerk"
            : ChatClerkProfiles.DisplayName(ActiveProfileKey);

    /// <summary>Host name used for machine mapping (Windows COMPUTERNAME when Web runs on the PC).</summary>
    public string MachineName { get; private set; } = Environment.MachineName;

    public bool IsMachineMapped { get; private set; }

    public bool IsManualOverride { get; private set; }

    /// <summary><c>machine</c>, <c>config</c>, <c>override</c>, or <c>unmapped</c>.</summary>
    public string ResolutionSource { get; private set; } = "unmapped";

    /// <summary>True when chat memory can isolate (known Deb/Paige profile).</summary>
    public bool IsResolved => ChatClerkProfiles.TryNormalize(ActiveProfileKey, out _);

    /// <summary>Value for <c>X-Tikr-Chat-User</c> when resolved.</summary>
    public string? HeaderValue => IsResolved ? ActiveProfileKey : null;

    /// <summary>Idempotent host resolution (config → machine map). Safe to call from layout/pages.</summary>
    public void EnsureResolved()
    {
        if (_initialized)
            return;
        ResolveFromHost();
    }

    /// <summary>
    /// Resolve from config/env, then machine name. Clears manual override.
    /// </summary>
    public void ResolveFromHost()
    {
        MachineName = Environment.MachineName;
        IsManualOverride = false;
        _initialized = true;

        var fromConfig = configuration[ChatClerkProfiles.EnvVarName]
            ?? configuration[ChatClerkProfiles.ConfigKey]
            ?? Environment.GetEnvironmentVariable(ChatClerkProfiles.EnvVarName);
        if (ChatClerkProfiles.TryNormalize(fromConfig, out var configKey))
        {
            ActiveProfileKey = configKey;
            IsMachineMapped = ChatClerkProfiles.TryResolveFromMachineName(MachineName, out var mapped)
                && mapped == configKey;
            ResolutionSource = "config";
            return;
        }

        if (ChatClerkProfiles.TryResolveFromMachineName(MachineName, out var machineKey))
        {
            ActiveProfileKey = machineKey;
            IsMachineMapped = true;
            ResolutionSource = "machine";
            return;
        }

        ActiveProfileKey = string.Empty;
        IsMachineMapped = false;
        ResolutionSource = "unmapped";
    }

    /// <summary>Rare Settings override (persisted in browser). Pass null/empty to clear.</summary>
    public void ApplyManualOverride(string? profileOrNull)
    {
        _initialized = true;
        if (string.IsNullOrWhiteSpace(profileOrNull))
        {
            ResolveFromHost();
            return;
        }

        if (!ChatClerkProfiles.TryNormalize(profileOrNull, out var key))
            return;

        MachineName = Environment.MachineName;
        ActiveProfileKey = key;
        IsManualOverride = true;
        ResolutionSource = "override";
        IsMachineMapped = ChatClerkProfiles.TryResolveFromMachineName(MachineName, out var mapped)
            && mapped == key;
    }

    public string StatusDetail()
    {
        if (ResolutionSource == "machine")
            return $"This computer ({MachineName}) is registered as {DisplayName} (NAS backup inventory).";
        if (ResolutionSource == "config")
            return $"Clerk set by install config: {DisplayName}.";
        if (ResolutionSource == "override")
            return $"Temporary override: {DisplayName}. Clear override in Settings to use machine mapping.";
        return $"This computer ({MachineName}) is not mapped to Deb Dillon or Paige Lindo. Choose a clerk in Settings for chat memory.";
    }
}
