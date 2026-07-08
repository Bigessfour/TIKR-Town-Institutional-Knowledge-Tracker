using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TIKR.Infrastructure.Services;

namespace TIKR.Infrastructure.Tests.Services;

public class SyncfusionDocumentLicenseTests
{
    [Fact]
    public void RegisterFromConfiguration_NoOpWhenKeyMissing()
    {
        var config = new ConfigurationBuilder().Build();

        var act = () => SyncfusionDocumentLicense.RegisterFromConfiguration(config);

        act.Should().NotThrow();
    }

    [Fact]
    public void RegisterFromConfiguration_AcceptsLicenseKey()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SYNCFUSION_LICENSE_KEY"] = "test-key" })
            .Build();

        var act = () => SyncfusionDocumentLicense.RegisterFromConfiguration(config);

        act.Should().NotThrow();
    }

    [Fact]
    public void GetStatus_WhenKeyMissing_ReportsNotConfigured()
    {
        var config = new ConfigurationBuilder().Build();

        var status = SyncfusionDocumentLicense.GetStatus(config);

        status.LicenseKeyConfigured.Should().BeFalse();
        status.LicenseProbePassed.Should().BeFalse();
        status.LicenseProbeDetail.Should().Contain("not set");
    }

    [Fact]
    public void GetStatus_WhenAgentToolsEnabled_ReflectsConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["USE_SYNCFUSION_AGENT_TOOLS"] = "true",
                ["USE_SYNCFUSION_AGENT_ORCHESTRATION"] = "true"
            })
            .Build();

        var status = SyncfusionDocumentLicense.GetStatus(config);

        status.AgentToolsEnabled.Should().BeTrue();
        status.OrchestrationEnabled.Should().BeTrue();
    }

    [Fact]
    public void GetStatus_WhenLicensed_PassesPdfProbe()
    {
        var licenseKey = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY");
        if (string.IsNullOrWhiteSpace(licenseKey))
            return;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SYNCFUSION_LICENSE_KEY"] = licenseKey })
            .Build();

        var status = SyncfusionDocumentLicense.GetStatus(config);

        status.LicenseKeyConfigured.Should().BeTrue();
        status.LicenseProbePassed.Should().BeTrue();
    }
}
