using FluentAssertions;
using TIKR.Shared.Helpers;

namespace TIKR.Shared.Tests.Helpers;

public class ProductHelpCatalogTests
{
    [Fact]
    public void Search_Redact_ReturnsSmartRedactHelp()
    {
        var hits = ProductHelpCatalog.Search("How do I use Smart Redact?", topK: 3);
        hits.Should().NotBeEmpty();
        hits.Should().Contain(h => h.Id == "smart-redact" || h.Title.Contains("Redact", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Search_SaveNas_ReturnsSaveHelp()
    {
        var hits = ProductHelpCatalog.Search("save PDF to NAS", topK: 3);
        hits.Should().Contain(h => h.Id == "save-to-nas");
    }

    [Fact]
    public void Search_LinkPacket_ReturnsLinkHelp()
    {
        var hits = ProductHelpCatalog.Search("link a packet to a due-out", topK: 3);
        hits.Should().Contain(h => h.Id == "link-packet");
    }

    [Fact]
    public void FormatForPrompt_IncludesProductHeader()
    {
        var hits = ProductHelpCatalog.Search("full screen", topK: 2);
        var block = ProductHelpCatalog.FormatForPrompt(hits);
        block.Should().Contain("TIKR product help");
        block.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void DefaultSuggestionChips_HasAtLeastFour()
    {
        ProductHelpCatalog.DefaultSuggestionChips.Count.Should().BeGreaterThanOrEqualTo(4);
    }
}
