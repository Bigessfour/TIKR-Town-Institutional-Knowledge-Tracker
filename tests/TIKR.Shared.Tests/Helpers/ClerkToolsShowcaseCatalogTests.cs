using FluentAssertions;
using TIKR.Shared.Helpers;

namespace TIKR.Shared.Tests.Helpers;

public class ClerkToolsShowcaseCatalogTests
{
    [Fact]
    public void Acts_AreOrderedAndCoverClerkRoutes()
    {
        ClerkToolsShowcaseCatalog.Acts.Should().HaveCountGreaterThanOrEqualTo(8);
        ClerkToolsShowcaseCatalog.Acts.Select(a => a.Order).Should().BeInAscendingOrder();
        ClerkToolsShowcaseCatalog.Acts.Select(a => a.Id).Should().OnlyHaveUniqueItems();

        var routes = ClerkToolsShowcaseCatalog.Acts.Select(a => a.Route).ToHashSet(StringComparer.Ordinal);
        routes.Should().Contain(["/", "/requirements", "/calendar", "/documents", "/assistant", "/vault", "/settings"]);
    }

    [Fact]
    public void SyncfusionSuggestedProcess_DocumentsLifecycleAndSmartAi()
    {
        var text = string.Join(' ', ClerkToolsShowcaseCatalog.SyncfusionSuggestedProcess);
        text.Should().ContainEquivalentOf("File Manager");
        text.Should().ContainEquivalentOf("AssistView");
        text.Should().ContainEquivalentOf("Sample Browser");
    }

    [Fact]
    public void GetById_IsCaseInsensitive()
    {
        ClerkToolsShowcaseCatalog.GetById("SMART-PDF").Should().NotBeNull();
        ClerkToolsShowcaseCatalog.GetById("missing").Should().BeNull();
    }

    [Fact]
    public void PageRoute_IsStable()
    {
        ClerkToolsShowcaseCatalog.PageRoute.Should().Be("/demo/clerk-tools");
    }
}
