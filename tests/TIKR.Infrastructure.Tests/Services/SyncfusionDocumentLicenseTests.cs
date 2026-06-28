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
}
