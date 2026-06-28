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
            await js.InvokeVoidAsync("tikrTheme.set", theme);
        }
        catch
        {
            // Theme persistence is best-effort when JS is unavailable.
        }

        Changed?.Invoke();
    }
}
