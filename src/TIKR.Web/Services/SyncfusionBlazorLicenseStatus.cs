namespace TIKR.Web.Services;

/// <summary>Startup result of Blazor platform license registration (Settings / ops visibility).</summary>
public sealed class SyncfusionBlazorLicenseStatus
{
    public bool KeyConfigured { get; init; }
    public bool BlazorLicenseValid { get; init; }
    public string? Detail { get; init; }
}
