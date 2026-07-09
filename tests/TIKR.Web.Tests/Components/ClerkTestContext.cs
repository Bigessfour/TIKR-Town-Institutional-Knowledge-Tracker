using Bunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Syncfusion.Blazor;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Components;

internal sealed class ClerkTestWebHostEnvironment : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "src", "TIKR.Web", "wwwroot"));

    public string ApplicationName { get; set; } = "TIKR.Web.Tests";
    public IFileProvider WebRootFileProvider { get; set; } = null!;
    public string EnvironmentName { get; set; } = "Test";
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = null!;
}

public abstract class ClerkTestContext : TestContext
{
    protected ClerkTestContext()
    {
        Services.AddSyncfusionBlazor();
        Services.AddSingleton(new SyncfusionBlazorLicenseStatus
        {
            KeyConfigured = true,
            BlazorLicenseValid = true,
            Detail = "Valid for Blazor UI (test host).",
        });
        Services.AddScoped<ClerkToastService>();
        Services.AddSingleton<LocalConnectionStateService>();
        Services.AddScoped<ThemeService>();
        Services.AddSingleton<IWebHostEnvironment>(new ClerkTestWebHostEnvironment());
        Services.AddScoped<ClerkUserGuideService>();
        Services.AddScoped<ClerkTourService>();
        Services.AddSingleton(new AuthSettings { IsEnabled = false });
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupVoid("tikrTour.run");
        JSInterop.Setup<bool>("tikrTour.getLocalFlag").SetResult(false);
        JSInterop.Setup<string?>("tikrTour.getLocalValue").SetResult(null);
    }
}
