using TIKR.Shared.Configuration;
using TIKR.Web;
using TIKR.Shared.Constants;
using TIKR.Web.Components;
using TIKR.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.AI;
using OllamaSharp;
using Syncfusion.Blazor;
using Syncfusion.Blazor.AI;
using Syncfusion.Blazor.SmartComponents;
using Serilog;
using Serilog.Events;

// Operational structured logging via Serilog (console + rolling file to /data/logs/tikr-*.log).
// Useful for runtime visibility, debugging production issues, and proof of operation.
// Verbosity: Debug (with Microsoft overrides to Warning).
try { Directory.CreateDirectory("/data/logs"); } catch { }
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "TIKR-Web")
    .WriteTo.Console()
    .WriteTo.File("/data/logs/tikr-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
    EnvLoader.LoadDevelopmentEnv(builder.Environment.ContentRootPath);

builder.Configuration.AddEnvironmentVariables();

// Persist antiforgery / Blazor circuit keys on NAS volume (docker: TIKR_DATA_PROTECTION_PATH=/data/.dpkeys).
var dataProtectionBuilder = builder.Services.AddDataProtection()
    .SetApplicationName("TIKR.Web");
var dataProtectionPath = builder.Configuration["TIKR_DATA_PROTECTION_PATH"];
if (!string.IsNullOrWhiteSpace(dataProtectionPath))
{
    try
    {
        Directory.CreateDirectory(dataProtectionPath);
        dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not persist data protection keys to {Path}", dataProtectionPath);
    }
}

// Syncfusion: register + validate Blazor license before AddSyncfusionBlazor (official startup order).
SyncfusionBlazorLicenseStatus blazorLicenseStatus;
using (var startupLogFactory = LoggerFactory.Create(b => b.AddConsole()))
{
    var startupLogger = startupLogFactory.CreateLogger("TIKR.Web.SyncfusionLicense");
    var keyConfigured = !string.IsNullOrWhiteSpace(TikrConfiguration.GetSyncfusionLicenseKey(builder.Configuration));
    var valid = SyncfusionLicenseBootstrap.RegisterIfConfigured(
        builder.Configuration,
        out var detail,
        startupLogger,
        "Blazor UI");
    blazorLicenseStatus = new SyncfusionBlazorLicenseStatus
    {
        KeyConfigured = keyConfigured,
        BlazorLicenseValid = valid,
        Detail = detail,
    };
}

builder.Services.AddSingleton(blazorLicenseStatus);

var authEnabled = TikrConfiguration.IsAuthEnabled(builder.Configuration);

var ollamaHost = TikrConfiguration.GetOllamaHost(builder.Configuration);
var chatModel = TikrConfiguration.GetChatModel(builder.Configuration);
var ollamaUri = ollamaHost.EndsWith('/') ? ollamaHost : ollamaHost + "/";

builder.Services.AddChatClient(_ =>
    new OllamaApiClient(new Uri(ollamaUri), chatModel));

// Register Syncfusion AI for Smart components (Paste / TextArea) via shared Ollama IChatClient.
builder.Services.AddSingleton<IChatInferenceService, SyncfusionAIService>();
builder.Services.AddSyncfusionSmartComponents().InjectOpenAIInference();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSyncfusionBlazor();
builder.Services.AddMemoryCache();

builder.Services.AddSingleton(new AuthSettings { IsEnabled = authEnabled });
builder.Services.AddSingleton<LocalConnectionStateService>();
builder.Services.AddScoped<ClerkToastService>();
builder.Services.AddScoped<ClerkUserGuideService>();
builder.Services.AddScoped<ClerkTourService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TikrAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<TikrAuthenticationStateProvider>());
builder.Services.AddScoped<IAuthSessionService, AuthSessionService>();
builder.Services.AddTransient<JwtAuthorizationHandler>();

if (authEnabled)
{
    builder.Services.AddAuthorizationCore(options =>
    {
        options.AddPolicy(TikrAuthPolicies.AdminOnly, policy => policy.RequireRole(TikrRoles.Admin));
    });
}

var resourceCatalogPath = Path.Combine(builder.Environment.ContentRootPath, "Data", "colorado-clerk-resources.json");
builder.Services.AddSingleton(ColoradoResourceCatalog.LoadFromFile(resourceCatalogPath));

var apiBaseUrl = builder.Configuration["TIKR_API_URL"] ?? "http://localhost:5000";
var apiUri = new Uri(apiBaseUrl.TrimEnd('/') + "/");

builder.Services.AddHttpClient("TikrAuth", client => client.BaseAddress = apiUri);
builder.Services.AddHttpClient<TikrApiClient>(client => client.BaseAddress = apiUri)
    .AddHttpMessageHandler<JwtAuthorizationHandler>();

// Serilog host integration for structured logging
builder.Host.UseSerilog();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

// Request logging via Serilog (captures HTTP interactions for observability)
app.UseSerilogRequestLogging();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

app.Run();
