using Syncfusion.Licensing;

namespace TIKR.Infrastructure.Services;

public static class SyncfusionDocumentLicense
{
    public static void RegisterFromConfiguration(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var licenseKey = configuration["SYNCFUSION_LICENSE_KEY"];
        if (!string.IsNullOrWhiteSpace(licenseKey))
            SyncfusionLicenseProvider.RegisterLicense(licenseKey);
    }
}
