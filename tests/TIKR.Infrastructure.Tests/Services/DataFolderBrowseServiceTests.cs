using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TIKR.Infrastructure.Services;
using TIKR.Shared.TestFixtures;

namespace TIKR.Infrastructure.Tests.Services;

[Trait("Category", TestCategories.FullyTested)]
public class DataFolderBrowseServiceTests
{
    [Fact]
    public void Browse_Roots_ListsConfiguredDataPath()
    {
        var root = Path.Combine(Path.GetTempPath(), "tikr-browse-root-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TIKR_DATA_PATH"] = root
                })
                .Build();
            var sut = new DataFolderBrowseService(config);

            var result = sut.Browse(null);

            result.Error.Should().BeNull();
            result.Roots.Should().Contain(r => Path.GetFullPath(r) == Path.GetFullPath(root));
            result.Entries.Should().Contain(e => Path.GetFullPath(e.FullPath) == Path.GetFullPath(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Browse_Children_FiltersByQuery()
    {
        var root = Path.Combine(Path.GetTempPath(), "tikr-browse-" + Guid.NewGuid().ToString("N"));
        var match = Path.Combine(root, "town-library");
        var other = Path.Combine(root, "other-stuff");
        Directory.CreateDirectory(match);
        Directory.CreateDirectory(other);

        try
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TIKR_DATA_PATH"] = root
                })
                .Build();
            var sut = new DataFolderBrowseService(config);

            var result = sut.Browse(root, "library");

            result.Error.Should().BeNull();
            result.CurrentPath.Should().Be(Path.GetFullPath(root));
            result.Entries.Should().ContainSingle(e => e.Name == "town-library");
            result.Entries.Should().NotContain(e => e.Name == "other-stuff");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Browse_OutsideRoots_ReturnsError()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TIKR_DATA_PATH"] = Path.Combine(Path.GetTempPath(), "tikr-allowed-" + Guid.NewGuid().ToString("N"))
            })
            .Build();
        Directory.CreateDirectory(config["TIKR_DATA_PATH"]!);
        try
        {
            var sut = new DataFolderBrowseService(config);
            var result = sut.Browse(Path.GetTempPath());

            result.Error.Should().NotBeNullOrWhiteSpace();
            result.Entries.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(config["TIKR_DATA_PATH"]!, recursive: true);
        }
    }
}
