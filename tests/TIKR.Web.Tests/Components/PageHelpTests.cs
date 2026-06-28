using Bunit;
using FluentAssertions;
using Syncfusion.Blazor;
using TIKR.Web.Components.Shared;

namespace TIKR.Web.Tests.Components;

public class PageHelpTests : TestContext
{
    public PageHelpTests()
    {
        Services.AddSyncfusionBlazor();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void PageHelp_RendersHelpButton()
    {
        var cut = RenderComponent<PageHelp>(p => p.Add(x => x.HelpText, "Upload documents here."));
        cut.Markup.Should().Contain("Help for this page");
        cut.Markup.Should().Contain("e-circle-info");
    }
}
