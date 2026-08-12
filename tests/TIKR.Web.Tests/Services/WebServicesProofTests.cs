using FluentAssertions;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using TIKR.Shared.Helpers;
using TIKR.Shared.TestFixtures;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Services;

[Trait("Category", TestCategories.FullyTested)]
public class ClerkUserGuideServiceTests
{
    [Fact]
    public async Task GetMarkdownAsync_LoadsGuideFromWebRoot()
    {
        var env = new FakeWebHostEnvironment();
        var guideDir = Path.Combine(env.WebRootPath, "help");
        Directory.CreateDirectory(guideDir);
        await File.WriteAllTextAsync(Path.Combine(guideDir, "clerk-user-guide.md"), "# Clerk guide\n\nTest content.");

        var sut = new ClerkUserGuideService(env);
        var md = await sut.GetMarkdownAsync();

        md.Should().Contain("Clerk guide");
        var sections = await sut.GetSectionsAsync();
        sections.Should().NotBeEmpty();
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public FakeWebHostEnvironment()
        {
            var root = Path.Combine(Path.GetTempPath(), $"tikr-wwwroot-{Guid.NewGuid():N}");
            WebRootPath = root;
            ContentRootPath = root;
        }

        public string ApplicationName { get; set; } = "TIKR.Web.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = null!;
        public string ContentRootPath { get; set; }
        public string EnvironmentName { get; set; } = Environments.Development;
        public IFileProvider WebRootFileProvider { get; set; } = null!;
        public string WebRootPath { get; set; }
    }
}

[Trait("Category", TestCategories.FullyTested)]
public class ChatClerkIdentityServiceTests
{
    [Fact]
    public void ResolveFromHost_UsesConfigOverride()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [ChatClerkProfiles.ConfigKey] = ChatClerkProfiles.Deb
            })
            .Build();

        var sut = new ChatClerkIdentityService(config);
        sut.EnsureResolved();

        sut.ActiveProfileKey.Should().Be(ChatClerkProfiles.Deb);
        sut.IsResolved.Should().BeTrue();
        sut.HeaderValue.Should().Be(ChatClerkProfiles.Deb);
        sut.StatusDetail().Should().Contain("Deb Dillon");
    }

    [Fact]
    public void ApplyManualOverride_SwitchesProfile_AndClearReverts()
    {
        var config = new ConfigurationBuilder().Build();
        var sut = new ChatClerkIdentityService(config);

        sut.ApplyManualOverride(ChatClerkProfiles.Paige);
        sut.ActiveProfileKey.Should().Be(ChatClerkProfiles.Paige);
        sut.IsManualOverride.Should().BeTrue();
        sut.ResolutionSource.Should().Be("override");

        sut.ApplyManualOverride(null);
        sut.IsManualOverride.Should().BeFalse();
    }
}

[Trait("Category", TestCategories.FullyTested)]
public class TikrCircuitHandlerTests
{
    // TikrCircuitHandler.OnCircuitOpenedAsync / OnCircuitClosedAsync / OnConnectionUpAsync /
    // OnConnectionDownAsync log circuit lifecycle for Interactive Server diagnostics.

    [Fact]
    public void TikrCircuitHandler_ExtendsCircuitHandler()
    {
        typeof(TikrCircuitHandler).Should().BeAssignableTo<CircuitHandler>();
    }
}
