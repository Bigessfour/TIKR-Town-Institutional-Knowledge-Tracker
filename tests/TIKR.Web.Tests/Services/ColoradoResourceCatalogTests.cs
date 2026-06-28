using FluentAssertions;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Services;

public class ColoradoResourceCatalogTests
{
    [Fact]
    public void ToSystemPromptBlock_EmptyCatalog_ReturnsEmptyString()
    {
        var catalog = new ColoradoResourceCatalog(Array.Empty<ColoradoResource>(), null);
        catalog.ToSystemPromptBlock().Should().BeEmpty();
    }

    [Fact]
    public void ToSystemPromptBlock_FormatsResourceWithUrl()
    {
        var catalog = new ColoradoResourceCatalog([
            new ColoradoResource("CML", "https://www.cml.org", "organization", ["governance"], "Trade association")
        ], "2026-01-01");

        var block = catalog.ToSystemPromptBlock();
        block.Should().Contain("CML");
        block.Should().Contain("https://www.cml.org");
        block.Should().Contain("Trade association");
    }

    [Fact]
    public void ToSystemPromptBlock_BlankUrl_UsesInternalContactPlaceholder()
    {
        var catalog = new ColoradoResourceCatalog([
            new ColoradoResource("Town Attorney", "", "contact", [], "Call for legal advice")
        ], null);

        catalog.ToSystemPromptBlock().Should().Contain("(internal contact)");
    }

    [Fact]
    public void LoadFromFile_MissingPath_ReturnsEmptyCatalog()
    {
        var catalog = ColoradoResourceCatalog.LoadFromFile(Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.json"));
        catalog.Resources.Should().BeEmpty();
        catalog.LastReviewed.Should().BeNull();
    }

    [Fact]
    public void LoadFromFile_ValidJson_LoadsResourcesAndDate()
    {
        var path = Path.Combine(Path.GetTempPath(), $"catalog-{Guid.NewGuid():N}.json");
        File.WriteAllText(path,
            """
            {
              "lastReviewed": "2026-06-28",
              "resources": [
                {
                  "name": "Test Org",
                  "url": "https://example.org",
                  "kind": "organization",
                  "topics": ["test"],
                  "summary": "Example resource"
                }
              ]
            }
            """);

        try
        {
            var catalog = ColoradoResourceCatalog.LoadFromFile(path);
            catalog.LastReviewed.Should().Be("2026-06-28");
            catalog.Resources.Should().ContainSingle().Which.Name.Should().Be("Test Org");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadFromFile_ShippedFixture_IsNonEmpty()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "TIKR.Web", "Data", "colorado-clerk-resources.json"));

        File.Exists(path).Should().BeTrue();
        var catalog = ColoradoResourceCatalog.LoadFromFile(path);
        catalog.ToSystemPromptBlock().Should().NotBeNullOrWhiteSpace();
    }
}
