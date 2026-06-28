using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using TIKR.Web.Components.Shared;

namespace TIKR.Web.Tests.Components;

public class TikrKeyboardShortcutsTests : ClerkTestContext
{
    public TikrKeyboardShortcutsTests() => SetRendererInfo(new RendererInfo("Server", true));

    [Fact]
    public void NavigateRequested_UpdatesLocation()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        var cut = RenderComponent<TikrKeyboardShortcuts>();

        cut.Instance.OnNavigateRequested("/requirements");

        nav.Uri.Should().EndWith("/requirements");
    }
}
