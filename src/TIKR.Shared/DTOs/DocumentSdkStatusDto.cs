namespace TIKR.Shared.DTOs;

public record DocumentSdkStatusDto(
    bool LicenseKeyConfigured,
    bool LicenseProbePassed,
    string? LicenseProbeDetail,
    bool AgentToolsEnabled,
    bool OrchestrationEnabled);
