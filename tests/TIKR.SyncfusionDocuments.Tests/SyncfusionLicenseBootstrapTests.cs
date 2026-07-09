using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TIKR.SyncfusionDocuments;

namespace TIKR.SyncfusionDocuments.Tests;

public class SyncfusionLicenseBootstrapTests
{
    [Fact]
    public void RegisterIfConfigured_ReturnsFalseWhenKeyMissing()
    {
        var config = new ConfigurationBuilder().Build();
        SyncfusionLicenseBootstrap.RegisterIfConfigured(config, componentLabel: "test").Should().BeFalse();
    }

    [Fact]
    public void RegisterIfConfigured_ReturnsTrueWhenKeyPresent()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SYNCFUSION_LICENSE_KEY"] = "unit-test-key" })
            .Build();
        SyncfusionLicenseBootstrap.RegisterIfConfigured(config, componentLabel: "test").Should().BeTrue();
    }
}
