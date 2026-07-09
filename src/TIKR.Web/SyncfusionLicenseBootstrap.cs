using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Syncfusion.Licensing;
using TIKR.Shared.Configuration;

namespace TIKR.Web;

public static class SyncfusionLicenseBootstrap
{
    public static bool RegisterIfConfigured(
        IConfiguration configuration,
        ILogger? logger = null,
        string componentLabel = "Syncfusion")
    {
        var key = TikrConfiguration.GetSyncfusionLicenseKey(configuration);
        if (string.IsNullOrWhiteSpace(key))
        {
            logger?.LogWarning("No Syncfusion license key found for {Component}", componentLabel);
            return false;
        }

        try
        {
            SyncfusionLicenseProvider.RegisterLicense(key);
            logger?.LogInformation(
                "Syncfusion license registered for {Component} (length: {Length})",
                componentLabel,
                key.Length);
            return true;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to register Syncfusion license for {Component}", componentLabel);
            return false;
        }
    }
}
