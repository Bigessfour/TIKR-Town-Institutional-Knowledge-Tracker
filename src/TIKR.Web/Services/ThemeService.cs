using Microsoft.JSInterop;

namespace TIKR.Web.Services;

public sealed class ThemeService(IJSRuntime js)
{
    public static IReadOnlyList<string> Options { get; } = ["light", "dark", "high-contrast"];

    public string Current { get; private set; } = "light";

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        try
        {
            Current = await js.InvokeAsync<string>("tikrTheme.get");
        }
        catch
        {
            Current = "light";
        }

        Changed?.Invoke();
    }

    public async Task SetThemeAsync(string theme)
    {
        if (!Options.Contains(theme))
            theme = "light";

        Current = theme;
        try
        {
            // Production guard: JS interop failures (timing, circuit state, missing functions)
            // must never produce an unhandled exception that triggers the bottom-left
            // "An unhandled error has occurred. Reload" banner.
            await js.InvokeVoidAsync("tikrTheme.set", theme);
        }
        catch
        {
            // Theme persistence + Syncfusion link swap is best-effort.
            // Our custom data-theme CSS rules still provide the visual switch for the sidebar.
        }

        Changed?.Invoke();
    }
}
