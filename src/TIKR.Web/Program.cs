using TIKR.Shared.Configuration;
using TIKR.Shared.Constants;
using TIKR.Web.Components;
using TIKR.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.AI;
using OllamaSharp;
using Syncfusion.Blazor;
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

var authEnabled = TikrConfiguration.IsAuthEnabled(builder.Configuration);

var ollamaHost = TikrConfiguration.GetOllamaHost(builder.Configuration);
var chatModel = TikrConfiguration.GetChatModel(builder.Configuration);
var ollamaUri = ollamaHost.EndsWith('/') ? ollamaHost : ollamaHost + "/";

builder.Services.AddChatClient(_ =>
    new OllamaApiClient(new Uri(ollamaUri), chatModel));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSyncfusionBlazor();

builder.Services.AddSingleton(new AuthSettings { IsEnabled = authEnabled });
builder.Services.AddSingleton<LocalConnectionStateService>();
builder.Services.AddScoped<ClerkToastService>();
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

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var syncfusionLicense = TikrConfiguration.GetSyncfusionLicenseKey(app.Configuration);
if (!string.IsNullOrWhiteSpace(syncfusionLicense))
{
    logger.LogInformation("Syncfusion license key found for Blazor UI (length: {Length})", syncfusionLicense.Length);
    Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionLicense);
}
else
{
    logger.LogWarning("No Syncfusion license key found for Blazor UI in configuration");
}

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
