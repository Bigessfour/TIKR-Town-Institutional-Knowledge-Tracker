using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Syncfusion.Licensing;
using TIKR.Shared.Configuration;

namespace TIKR.Web;

public static class SyncfusionLicenseBootstrap
{
    private static readonly Platform[] BlazorPlatforms = [Platform.Blazor];

    public static bool RegisterIfConfigured(
        IConfiguration configuration,
        out string? validationDetail,
        ILogger? logger = null,
        string componentLabel = "Syncfusion")
    {
        validationDetail = null;
        var key = TikrConfiguration.GetSyncfusionLicenseKey(configuration);
        if (string.IsNullOrWhiteSpace(key))
        {
            logger?.LogWarning("No Syncfusion license key found for {Component}", componentLabel);
            validationDetail = "SYNCFUSION_LICENSE_KEY is not set.";
            return false;
        }

        try
        {
            key = key.Trim();

            string? validationError = null;
            SyncfusionLicenseProvider.RegisterLicense(key);
            if (!SyncfusionLicenseProvider.ValidateLicense(BlazorPlatforms, out validationError))
            {
                validationDetail = validationError ?? "License is not valid for Blazor v34.x.";
                logger?.LogError(
                    "Syncfusion license validation failed for {Component}: {Detail}. " +
                    "Claim a Blazor v34.x key at https://www.syncfusion.com/account/downloads (version and platform must match NuGet 34.1.29).",
                    componentLabel,
                    validationDetail);
                return false;
            }

            validationDetail = "Valid for Blazor UI (v34.1.x packages).";
            logger?.LogInformation(
                "Syncfusion license registered and validated for {Component} (length: {Length})",
                componentLabel,
                key.Length);
            return true;
        }
        catch (Exception ex)
        {
            validationDetail = ex.Message;
            logger?.LogError(ex, "Failed to register Syncfusion license for {Component}", componentLabel);
            return false;
        }
    }

    public static bool RegisterIfConfigured(
        IConfiguration configuration,
        ILogger? logger = null,
        string componentLabel = "Syncfusion") =>
        RegisterIfConfigured(configuration, out _, logger, componentLabel);
}
