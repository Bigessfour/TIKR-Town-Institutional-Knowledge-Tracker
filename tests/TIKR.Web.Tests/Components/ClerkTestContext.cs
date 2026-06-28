using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Blazor;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

public abstract class ClerkTestContext : TestContext
{
    protected ClerkTestContext()
    {
        Services.AddSyncfusionBlazor();
        Services.AddScoped<ClerkToastService>();
        Services.AddSingleton<LocalConnectionStateService>();
        Services.AddScoped<ThemeService>();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
