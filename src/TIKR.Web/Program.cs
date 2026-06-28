using TIKR.Shared.Configuration;
using TIKR.Shared.Constants;
using TIKR.Web.Components;
using TIKR.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.AI;
using OllamaSharp;
using Syncfusion.Blazor;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
    EnvLoader.LoadDevelopmentEnv(builder.Environment.ContentRootPath);

builder.Configuration.AddEnvironmentVariables();

var authEnabled = TikrConfiguration.IsAuthEnabled(builder.Configuration);

var syncfusionLicense = builder.Configuration["SYNCFUSION_LICENSE_KEY"];
if (!string.IsNullOrWhiteSpace(syncfusionLicense))
    Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(syncfusionLicense);

var ollamaHost = TikrConfiguration.GetOllamaHost(builder.Configuration);
var chatModel = TikrConfiguration.GetChatModel(builder.Configuration);
var ollamaUri = ollamaHost.EndsWith('/') ? ollamaHost : ollamaHost + "/";

builder.Services.AddChatClient(_ =>
    new OllamaApiClient(new Uri(ollamaUri), chatModel));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSyncfusionBlazor();
builder.Services.AddSingleton(new AuthSettings { IsEnabled = authEnabled });
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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
