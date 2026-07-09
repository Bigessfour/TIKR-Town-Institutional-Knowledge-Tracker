using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TIKR.Web;

namespace TIKR.Web.Tests;

public class SyncfusionLicenseBootstrapTests
{
    [Fact]
    public void RegisterIfConfigured_ReturnsFalseWhenKeyMissing()
    {
        var config = new ConfigurationBuilder().Build();
        SyncfusionLicenseBootstrap.RegisterIfConfigured(config, componentLabel: "Blazor UI").Should().BeFalse();
    }

    [Fact]
    public void RegisterIfConfigured_ReturnsFalseWhenKeyIsNotValidForBlazorPlatform()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["SYNCFUSION_LICENSE_KEY"] = "not-a-real-syncfusion-key" })
            .Build();
        SyncfusionLicenseBootstrap.RegisterIfConfigured(config, componentLabel: "Blazor UI").Should().BeFalse();
    }
}
