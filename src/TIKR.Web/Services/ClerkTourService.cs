using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TIKR.Shared.DTOs;
using TIKR.Web.ClerkTour;

namespace TIKR.Web.Services;

public sealed class ClerkTourService(
    IJSRuntime js,
    NavigationManager navigation,
    AuthSettings auth,
    TikrApiClient api)
{
    public const string LocalCompletedKey = "tikr-tour-completed-version";
    public const string LocalAutoDisabledKey = "tikr-tour-auto-disabled";

    private DotNetObjectReference<ClerkTourJsCallbacks>? _bridgeRef;
    private bool _running;

    public event Action? StateChanged;

    public bool IsRunning => _running;

    public Task<bool> IsAutoTourDisabledForUiAsync() => IsAutoTourDisabledAsync();

    public async Task<bool> ShouldAutoStartAsync()
    {
        if (await IsAutoTourDisabledAsync())
            return false;

        var completed = await GetCompletedVersionAsync();
        return !string.Equals(completed, ClerkTourCatalog.CurrentVersion, StringComparison.Ordinal);
    }

    public async Task StartGlobalTourAsync() =>
        await RunTourAsync(ClerkTourCatalog.GetGlobalSteps());

    public async Task StartFullTourAsync() =>
        await RunTourAsync(ClerkTourCatalog.GetFullTourSteps());

    public async Task StartPageTourAsync(string route)
    {
        var normalized = string.IsNullOrWhiteSpace(route) ? "/" : route.Split('?')[0];
        var steps = ClerkTourCatalog.GetPageSteps(normalized);
        if (steps.Count == 0)
            return;

        if (!navigation.Uri.Contains(normalized, StringComparison.OrdinalIgnoreCase))
            navigation.NavigateTo(normalized);

        await RunTourAsync(steps);
    }

    public async Task SetAutoTourDisabledAsync(bool disabled)
    {
        if (auth.IsEnabled && await TryUpdateServerTourAsync(s => s with { AutoTourDisabled = disabled }))
            return;

        await js.InvokeVoidAsync("tikrTour.setLocalFlag", LocalAutoDisabledKey, disabled);
    }

    public async Task MarkCompletedAsync()
    {
        if (auth.IsEnabled && await TryUpdateServerTourAsync(s => s with { CompletedVersion = ClerkTourCatalog.CurrentVersion }))
            return;

        await js.InvokeVoidAsync("tikrTour.setLocalValue", LocalCompletedKey, ClerkTourCatalog.CurrentVersion);
    }

    private async Task<bool> TryUpdateServerTourAsync(Func<ClerkTourStateDto, ClerkTourStateDto> update)
    {
        var current = await api.GetClerkTourStateAsync();
        if (current is null)
            return false;

        var updated = await api.UpdateClerkTourStateAsync(update(current));
        return updated is not null;
    }

    private async Task RunTourAsync(IReadOnlyList<ClerkTourStep> steps)
    {
        if (_running)
            return;

        _running = true;
        StateChanged?.Invoke();
        _bridgeRef?.Dispose();
        _bridgeRef = DotNetObjectReference.Create(new ClerkTourJsCallbacks(this, navigation));

        try
        {
            var dto = steps.Select(s => new { element = s.Element, title = s.Title, description = s.Description, route = s.Route }).ToList();
            await js.InvokeVoidAsync("tikrTour.run", dto, _bridgeRef);
        }
        catch
        {
            _running = false;
            StateChanged?.Invoke();
        }
    }

    internal async Task OnTourFinishedFromJsAsync()
    {
        _running = false;
        await MarkCompletedAsync();
        _bridgeRef?.Dispose();
        _bridgeRef = null;
        StateChanged?.Invoke();
    }

    private async Task<bool> IsAutoTourDisabledAsync()
    {
        if (auth.IsEnabled)
        {
            var state = await api.GetClerkTourStateAsync();
            if (state is not null)
                return state.AutoTourDisabled;
        }

        try
        {
            return await js.InvokeAsync<bool>("tikrTour.getLocalFlag", LocalAutoDisabledKey);
        }
        catch
        {
            return false;
        }
    }

    private async Task<string?> GetCompletedVersionAsync()
    {
        if (auth.IsEnabled)
        {
            var state = await api.GetClerkTourStateAsync();
            if (state?.CompletedVersion is not null)
                return state.CompletedVersion;
        }

        try
        {
            return await js.InvokeAsync<string?>("tikrTour.getLocalValue", LocalCompletedKey);
        }
        catch
        {
            return null;
        }
    }

    public sealed class ClerkTourJsCallbacks(ClerkTourService owner, NavigationManager navigation)
    {
        [JSInvokable]
        public Task NavigateForTourAsync(string route)
        {
            if (!string.IsNullOrWhiteSpace(route))
            {
                var target = navigation.ToAbsoluteUri(route).ToString();
                if (!navigation.Uri.Equals(target, StringComparison.OrdinalIgnoreCase))
                    navigation.NavigateTo(route);
            }

            return Task.CompletedTask;
        }

        [JSInvokable]
        public Task OnTourFinishedFromJs() => owner.OnTourFinishedFromJsAsync();
    }
}
