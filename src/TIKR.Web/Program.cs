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

// Operational structured logging via Serilog (console + rolling file).
// Prefer NAS /data/logs; fall back to repo .local-data/logs on Mac/dev so click-through errors are greppable.
static string ResolveWebLogDirectory()
{
    foreach (var candidate in new[]
             {
                 "/data/logs",
                 Environment.GetEnvironmentVariable("TIKR_LOG_PATH"),
                 Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".local-data", "logs")),
                 Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".local-data", "logs")),
                 Path.Combine(Path.GetTempPath(), "tikr-logs")
             })
    {
        if (string.IsNullOrWhiteSpace(candidate)) continue;
        try
        {
            Directory.CreateDirectory(candidate);
            return candidate;
        }
        catch { /* try next */ }
    }
    return Path.GetTempPath();
}

var webLogDir = ResolveWebLogDirectory();
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    // Surface Blazor circuit / render exceptions at Error for Documents click-through debugging.
    .MinimumLevel.Override("Microsoft.AspNetCore.Components", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore.Components.Server", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore.SignalR", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "TIKR-Web")
    .WriteTo.Console()
    .WriteTo.File(Path.Combine(webLogDir, "tikr-web-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();
Log.Information("TIKR.Web Serilog writing to {LogDirectory}", webLogDir);

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
    EnvLoader.LoadDevelopmentEnv(builder.Environment.ContentRootPath);
else
    EnvLoader.LoadRuntimeSecrets(builder.Configuration["TIKR_DATA_PATH"]);

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

// Large document preview / editor payloads (PDF bytes, SFDT) need a bigger SignalR frame.
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 100 * 1024 * 1024;
});

// Circuit lifecycle + connection-down warnings → Serilog (grep CircuitId=).
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.Circuits.CircuitHandler, TikrCircuitHandler>();

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
builder.Services.AddScoped<ChatClerkIdentityService>();
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
// Log outbound API proxy traffic from the Web host (clerk button → Web → API).
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0} ms";
    options.GetLevel = (httpContext, elapsed, ex) =>
        ex is not null || httpContext.Response.StatusCode >= 500
            ? Serilog.Events.LogEventLevel.Error
            : httpContext.Response.StatusCode >= 400
                ? Serilog.Events.LogEventLevel.Warning
                : httpContext.Request.Method is "GET" or "HEAD"
                    ? Serilog.Events.LogEventLevel.Debug
                    : Serilog.Events.LogEventLevel.Information;
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

app.Run();
