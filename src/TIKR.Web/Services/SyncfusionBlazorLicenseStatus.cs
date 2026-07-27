namespace TIKR.Web.Services;

/// <summary>Startup result of Blazor platform license registration (Settings / ops visibility).</summary>
public sealed class SyncfusionBlazorLicenseStatus
{
    public bool KeyConfigured { get; set; }
    public bool BlazorLicenseValid { get; set; }
    public string? Detail { get; set; }
}
