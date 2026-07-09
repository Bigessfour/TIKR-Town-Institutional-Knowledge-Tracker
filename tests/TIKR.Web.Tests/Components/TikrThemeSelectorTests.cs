using AngleSharp.Dom;
using Bunit;
using TIKR.Web.Components.Shared;

namespace TIKR.Web.Tests.Components;

public sealed class TikrThemeSelectorTests : ClerkTestContext
{
    public TikrThemeSelectorTests()
    {
        JSInterop.Setup<string>("tikrTheme.get").SetResult("light");
        JSInterop.SetupVoid("tikrTheme.set", _ => true);
    }

    [Fact]
    public void Renders_native_select_with_three_theme_options()
    {
        var cut = RenderComponent<TikrThemeSelector>();

        var select = cut.Find("select#tikr-theme-ddl");
        var options = select.QuerySelectorAll("option");
        Assert.Equal(3, options.Length);
        Assert.Collection(
            options,
            o => Assert.Equal(("light", "Light"), (o.GetAttribute("value"), o.TextContent)),
            o => Assert.Equal(("dark", "Dark"), (o.GetAttribute("value"), o.TextContent)),
            o => Assert.Equal(("high-contrast", "High contrast"), (o.GetAttribute("value"), o.TextContent)));
    }
}
