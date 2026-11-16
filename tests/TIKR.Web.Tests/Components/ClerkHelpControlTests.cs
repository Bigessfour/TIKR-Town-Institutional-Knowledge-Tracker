using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using TIKR.Web.Components.Shared;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

public class ClerkHelpControlTests : ClerkTestContext
{
    public ClerkHelpControlTests() => SetRendererInfo(new RendererInfo("Server", true));

    [Fact]
    public void PageHelp_ClickShowsTheHelpText()
    {
        var cut = RenderComponent<PageHelp>(p => p.Add(c => c.HelpText, "How deadlines work"));
        cut.FindAll("[role=status]").Should().BeEmpty();

        cut.Find("button").Click();

        cut.Find("[role=status]").TextContent.Should().Contain("How deadlines work");
    }

    [Fact]
    public async Task UserGuide_OpensAgainAfterItCloses()
    {
        var cut = RenderComponent<ClerkUserGuideDialog>();
        var guide = Services.GetRequiredService<ClerkUserGuideService>();

        await cut.InvokeAsync(() => guide.Open());
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("tikr-guide-search"));

        var close = cut.Find(".e-dlg-closeicon-btn");
        await cut.InvokeAsync(() => close.Click());
        cut.WaitForAssertion(() => cut.FindAll(".e-dlg-closeicon-btn").Should().BeEmpty());

        await cut.InvokeAsync(() => guide.Open());
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("tikr-guide-search"));
    }
}
