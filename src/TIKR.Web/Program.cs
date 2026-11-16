using TIKR.Shared.Configuration;
using TIKR.Shared.Helpers;
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
var cognitoGate = CognitoSilentAuth.HostedUiDomain(builder.Configuration) is not null;

var ollamaHost = TikrConfiguration.GetOllamaHost(builder.Configuration);
var chatModel = TikrConfiguration.GetChatModel(builder.Configuration);
var ollamaUri = ollamaHost.EndsWith('/') ? ollamaHost : ollamaHost + "/";

var chatTimeout = TimeSpan.FromSeconds(
    TikrConfiguration.GetChatCompletionTimeoutSeconds(builder.Configuration) + 30);
var ollamaHttp = new HttpClient
{
    BaseAddress = new Uri(ollamaUri),
    Timeout = chatTimeout
};
builder.Services.AddChatClient(_ => new OllamaApiClient(ollamaHttp, chatModel));

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

builder.Services.AddSingleton(new AuthSettings
{
    IsEnabled = authEnabled,
    CognitoGateEnabled = cognitoGate
});
builder.Services.AddHttpClient("CognitoJwks");
builder.Services.AddSingleton<CognitoIdTokenValidator>();
builder.Services.AddSingleton<LocalConnectionStateService>();
builder.Services.AddScoped<ClerkToastService>();
builder.Services.AddScoped<ClerkUserGuideService>();
builder.Services.AddScoped<ClerkTourService>();
builder.Services.AddScoped<ThemeService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TikrAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<TikrAuthenticationStateProvider>());
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.IHostEnvironmentAuthenticationStateProvider>(sp =>
    sp.GetRequiredService<TikrAuthenticationStateProvider>());
builder.Services.AddScoped<IAuthSessionService, AuthSessionService>();
builder.Services.AddScoped<ChatClerkIdentityService>();
builder.Services.AddTransient<JwtAuthorizationHandler>();

builder.Services.AddAuthorization(options =>
{
    if (authEnabled)
    {
        options.AddPolicy(TikrAuthPolicies.AdminOnly, policy => policy.RequireRole(TikrRoles.Admin));
    }
    else if (cognitoGate)
    {
        options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
        options.AddPolicy(TikrAuthPolicies.AdminOnly, policy =>
            policy.RequireRole("admin", "Admin"));
    }
    else
    {
        // No town sign-in configured. [Authorize] pages must still render.
        var allow = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true)
            .Build();
        options.DefaultPolicy = allow;
        options.AddPolicy(TikrAuthPolicies.AdminOnly, policy => policy.RequireAssertion(_ => true));
    }
});

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
app.UseAuthorization();
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

app.Use(async (context, next) =>
{
    var config = context.RequestServices.GetRequiredService<IConfiguration>();
    if (CognitoSilentAuth.HostedUiDomain(config) is not null)
    {
        var validator = context.RequestServices.GetRequiredService<CognitoIdTokenValidator>();
        if (context.Request.Cookies.TryGetValue(AuthCookie.CognitoSession, out var sessionToken)
            && !string.IsNullOrWhiteSpace(sessionToken))
        {
            var principal = await validator.ValidateAsync(sessionToken, context.RequestAborted);
            if (principal is not null && CognitoAccess.IsAllowed(principal))
            {
                context.User = principal;
            }
            else if (CognitoSilentAuth.IsDocumentNavigation(context.Request))
            {
                var denied = principal is not null
                    ? "/auth/cognito-denied?reason=role"
                    : "/auth/cognito-start?return=" + Uri.EscapeDataString(
                        CognitoSilentAuth.SafeReturnPath(context.Request.Path + context.Request.QueryString));
                context.Response.Redirect(denied);
                return;
            }
        }
        else if (CognitoSilentAuth.ShouldProbe(context.Request, config))
        {
            var returnPath = CognitoSilentAuth.SafeReturnPath(context.Request.Path + context.Request.QueryString);
            context.Response.Redirect("/auth/cognito-start?return=" + Uri.EscapeDataString(returnPath));
            return;
        }
    }

    await next();
});

app.MapGet("/auth/cognito-start", (HttpContext http, IConfiguration config) =>
{
    var returnPath = CognitoSilentAuth.SafeReturnPath(http.Request.Query["return"]);
    var interactive = string.Equals(http.Request.Query["mode"], "login", StringComparison.OrdinalIgnoreCase);
    var shortCookie = CognitoSilentAuth.SessionCookie(http.Request, config);
    shortCookie.MaxAge = TimeSpan.FromMinutes(10);
    http.Response.Cookies.Append(AuthCookie.AuthReturn, returnPath, shortCookie);

    var domain = CognitoSilentAuth.HostedUiDomain(config);
    if (domain is null)
        return Results.Redirect(returnPath);

    var verifier = CognitoSilentAuth.CreateCodeVerifier();
    var state = CognitoSilentAuth.CreateState();
    http.Response.Cookies.Append(AuthCookie.PkceVerifier, verifier + "." + state, shortCookie);
    if (interactive)
        http.Response.Cookies.Delete(AuthCookie.CognitoSilent);
    else
        http.Response.Cookies.Append(AuthCookie.CognitoSilent, "1", shortCookie);

    var url = CognitoSilentAuth.BuildAuthorizeUrl(
        domain,
        CognitoSilentAuth.ClientId(config),
        CognitoSilentAuth.RedirectUri(http.Request, config),
        state,
        CognitoSilentAuth.CreateCodeChallenge(verifier),
        prompt: interactive ? null : "none");
    return Results.Redirect(url);
});

app.MapGet("/auth/cognito-callback", async (
    HttpContext http,
    IConfiguration config,
    IHttpClientFactory httpFactory,
    CognitoIdTokenValidator validator,
    CancellationToken ct) =>
{
    var returnPath = http.Request.Cookies.TryGetValue(AuthCookie.AuthReturn, out var saved)
        ? CognitoSilentAuth.SafeReturnPath(saved)
        : "/";
    var silent = http.Request.Cookies.ContainsKey(AuthCookie.CognitoSilent);
    var session = CognitoSilentAuth.SessionCookie(http.Request, config);
    http.Response.Cookies.Delete(AuthCookie.PkceVerifier);
    http.Response.Cookies.Delete(AuthCookie.AuthReturn);
    http.Response.Cookies.Delete(AuthCookie.CognitoSilent);
    http.Response.Cookies.Delete(AuthCookie.CognitoChecked);

    var error = http.Request.Query["error"].ToString();
    if (silent && CognitoSilentAuth.IsSilentRetry(error))
    {
        return Results.Redirect(
            "/auth/cognito-start?mode=login&return=" + Uri.EscapeDataString(returnPath));
    }

    var code = http.Request.Query["code"].ToString();
    var state = http.Request.Query["state"].ToString();
    var domain = CognitoSilentAuth.HostedUiDomain(config);
    if (!string.IsNullOrEmpty(error) || domain is null || string.IsNullOrEmpty(code)
        || !http.Request.Cookies.TryGetValue(AuthCookie.PkceVerifier, out var packed))
    {
        return Results.Redirect("/auth/cognito-denied");
    }

    var dot = packed.IndexOf('.');
    if (dot <= 0 || !string.Equals(packed[(dot + 1)..], state, StringComparison.Ordinal))
        return Results.Redirect("/auth/cognito-denied");

    var idToken = await CognitoSilentAuth.ExchangeCodeAsync(
        httpFactory.CreateClient(),
        domain,
        CognitoSilentAuth.ClientId(config),
        config[CognitoSilentAuth.ClientSecretConfigKey],
        CognitoSilentAuth.RedirectUri(http.Request, config),
        code,
        packed[..dot],
        ct);
    if (string.IsNullOrWhiteSpace(idToken))
        return Results.Redirect("/auth/cognito-denied");

    var principal = await validator.ValidateAsync(idToken, ct);
    if (principal is null
        || !CognitoSilentAuth.TryReadIdentity(idToken, CognitoSilentAuth.ClientId(config), out var sub, out var email))
    {
        return Results.Redirect("/auth/cognito-denied");
    }

    if (!CognitoAccess.IsAllowed(principal))
        return Results.Redirect("/auth/cognito-denied?reason=role");

    http.Response.Cookies.Append(AuthCookie.CognitoSession, idToken, session);
    http.Response.Cookies.Append(AuthCookie.ChatUserName, CognitoChatIdentity.Format(sub), session);
    if (!string.IsNullOrWhiteSpace(email))
        http.Response.Cookies.Append(AuthCookie.ChatDisplayName, email, session);

    return Results.Redirect(returnPath);
});

app.MapGet("/auth/cognito-logout", (HttpContext http, IConfiguration config) =>
{
    foreach (var name in new[]
             {
                 AuthCookie.CognitoSession,
                 AuthCookie.ChatUserName,
                 AuthCookie.ChatDisplayName,
                 AuthCookie.CognitoSilent,
                 AuthCookie.CognitoChecked,
                 AuthCookie.PkceVerifier,
                 AuthCookie.AuthReturn,
                 AuthCookie.Name
             })
    {
        http.Response.Cookies.Delete(name);
    }

    var domain = CognitoSilentAuth.HostedUiDomain(config);
    if (domain is null)
        return Results.Redirect("/");

    return Results.Redirect(CognitoSilentAuth.BuildLogoutUrl(
        domain,
        CognitoSilentAuth.ClientId(config),
        CognitoSilentAuth.LogoutUri(http.Request, config)));
});

app.MapGet("/auth/cognito-denied", (string? reason) =>
{
    var message = string.Equals(reason, "role", StringComparison.OrdinalIgnoreCase)
        ? "This town account does not include TIKR. Ask Steve if you need it."
        : "The town sign-in did not finish. Try again from the town home page.";
    var html = $"""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="utf-8"><title>TIKR sign-in</title></head>
        <body style="font-family: sans-serif; margin: 2rem;">
        <h1>TIKR</h1>
        <p>{message}</p>
        <p><a href="/auth/cognito-start?mode=login&amp;return=/">Sign in again</a></p>
        </body>
        </html>
        """;
    return Results.Content(html, "text/html");
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Lifetime.ApplicationStopped.Register(Log.CloseAndFlush);

app.Run();
