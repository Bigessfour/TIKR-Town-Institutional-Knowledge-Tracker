using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using TIKR.Web.Components.Shared;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

public class TikrOfflineBannerTests : ClerkTestContext
{
    public TikrOfflineBannerTests() => SetRendererInfo(new RendererInfo("Server", true));

    [Fact]
    public void OfflineBanner_ShowsWhenApiOffline()
    {
        var connection = Services.GetRequiredService<LocalConnectionStateService>();
        var cut = RenderComponent<TikrOfflineBanner>();

        connection.SetApiOffline(true);
        cut.Render();

        cut.Markup.Should().Contain("API offline");
        cut.Markup.Should().Contain("still on the NAS");
    }

    [Fact]
    public void OfflineBanner_HiddenWhenApiOnline()
    {
        var connection = Services.GetRequiredService<LocalConnectionStateService>();
        connection.SetApiOffline(false);

        var cut = RenderComponent<TikrOfflineBanner>();
        cut.Markup.Should().NotContain("API offline");
    }
}
