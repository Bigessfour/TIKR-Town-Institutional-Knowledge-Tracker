using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TIKR.Shared.Helpers;

namespace TIKR.Shared.Tests.Helpers;

public class TikrDataPathRootsTests
{
    [Theory]
    [InlineData("Macintosh HD", true)]
    [InlineData("Macintosh HD - Data", true)]
    [InlineData("Recovery", true)]
    [InlineData("com.apple.TimeMachine.localsnapshots", true)]
    [InlineData("Backups of Steve's Mac", true)]
    [InlineData(".hidden", true)]
    [InlineData("External HD", false)]
    [InlineData("Town NAS", false)]
    [InlineData("volume1", false)]
    public void IsExcludedVolumeName_FiltersSystemVolumes(string name, bool excluded) =>
        TikrDataPathRoots.IsExcludedVolumeName(name).Should().Be(excluded);

    [Fact]
    public void Resolve_IncludesConfiguredDataPathAndExtraRoots()
    {
        var data = Path.Combine(Path.GetTempPath(), "tikr-roots-data-" + Guid.NewGuid().ToString("N"));
        var extra = Path.Combine(Path.GetTempPath(), "tikr-roots-extra-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(data);
        Directory.CreateDirectory(extra);
        try
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TIKR_DATA_PATH"] = data,
                    [TikrDataPathRoots.ExtraRootsConfigKey] = extra
                })
                .Build();

            var roots = TikrDataPathRoots.Resolve(config, currentDirectory: data);

            roots.Should().Contain(r => Path.GetFullPath(r) == Path.GetFullPath(data));
            roots.Should().Contain(r => Path.GetFullPath(r) == Path.GetFullPath(extra));
            TikrDataPathRoots.IsUnderAllowedRoot(Path.Combine(extra, "town"), roots).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(data, recursive: true);
            Directory.Delete(extra, recursive: true);
        }
    }

    [Fact]
    public void Resolve_IncludesMountedExternalVolume_WhenPresent()
    {
        if (!Directory.Exists("/Volumes/External HD"))
            return; // machine-specific; skip when drive not mounted

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var roots = TikrDataPathRoots.Resolve(config);

        roots.Should().Contain(r =>
            Path.GetFullPath(r).Equals(Path.GetFullPath("/Volumes/External HD"), StringComparison.OrdinalIgnoreCase));
        roots.Should().NotContain(r => Path.GetFullPath(r) == "/");
    }
}
