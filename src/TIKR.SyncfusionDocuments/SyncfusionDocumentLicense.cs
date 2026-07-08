using Syncfusion.Licensing;
using Syncfusion.Pdf;
using TIKR.Shared.Configuration;
using TIKR.Shared.DTOs;

namespace TIKR.SyncfusionDocuments;

public static class SyncfusionDocumentLicense
{
    public static void RegisterFromConfiguration(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var licenseKey = TikrConfiguration.GetSyncfusionLicenseKey(configuration);
        if (!string.IsNullOrWhiteSpace(licenseKey))
            SyncfusionLicenseProvider.RegisterLicense(licenseKey);
    }

    public static DocumentSdkStatusDto GetStatus(Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var keyConfigured = !string.IsNullOrWhiteSpace(TikrConfiguration.GetSyncfusionLicenseKey(configuration));
        var agentToolsEnabled = TikrConfiguration.GetUseSyncfusionAgentTools(configuration);
        var orchestrationEnabled = TikrConfiguration.GetUseSyncfusionAgentOrchestration(configuration);

        if (!keyConfigured)
        {
            return new DocumentSdkStatusDto(
                LicenseKeyConfigured: false,
                LicenseProbePassed: false,
                LicenseProbeDetail: "SYNCFUSION_LICENSE_KEY is not set.",
                AgentToolsEnabled: agentToolsEnabled,
                OrchestrationEnabled: orchestrationEnabled);
        }

        RegisterFromConfiguration(configuration);

        try
        {
            ProbePdfCreation();
            return new DocumentSdkStatusDto(
                LicenseKeyConfigured: true,
                LicenseProbePassed: true,
                LicenseProbeDetail: null,
                AgentToolsEnabled: agentToolsEnabled,
                OrchestrationEnabled: orchestrationEnabled);
        }
        catch (Exception ex)
        {
            return new DocumentSdkStatusDto(
                LicenseKeyConfigured: true,
                LicenseProbePassed: false,
                LicenseProbeDetail: ex.Message,
                AgentToolsEnabled: agentToolsEnabled,
                OrchestrationEnabled: orchestrationEnabled);
        }
    }

    private static void ProbePdfCreation()
    {
        using var document = new PdfDocument();
        document.Pages.Add();
        using var stream = new MemoryStream();
        document.Save(stream);

        if (stream.Length < 64)
            throw new InvalidOperationException("Document SDK PDF probe produced no output.");
    }
}
