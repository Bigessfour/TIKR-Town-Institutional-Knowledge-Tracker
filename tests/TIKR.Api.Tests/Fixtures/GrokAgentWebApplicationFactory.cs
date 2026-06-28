using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TIKR.Api.Tests.Fixtures;

/// <summary>
/// API test host with xAI Grok enabled. Requires <c>GROK_API_KEY</c> or <c>XAI_API_KEY</c> for live validation.
/// </summary>
public class GrokAgentWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"tikr-grok-api-{Guid.NewGuid():N}.db");
    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), $"tikr-grok-api-storage-{Guid.NewGuid():N}");

    internal static bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ResolveGrokApiKey());

    internal static string? ResolveGrokApiKey() =>
        Environment.GetEnvironmentVariable("GROK_API_KEY")
        ?? Environment.GetEnvironmentVariable("XAI_API_KEY");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var grokKey = ResolveGrokApiKey();
        var grokModel = Environment.GetEnvironmentVariable("GROK_MODEL") ?? "grok-3";

        builder.UseEnvironment("Testing");
        builder.UseSetting("TIKR_AUTH_ENABLED", "false");
        builder.UseSetting("USE_GROK", "true");
        builder.UseSetting("AI:UseGrok", "true");
        builder.UseSetting("GROK_API_KEY", grokKey);
        builder.UseSetting("GROK_MODEL", grokModel);
        builder.UseSetting("AI:GrokModel", grokModel);
        builder.UseSetting("OLLAMA_HOST", "http://127.0.0.1:1");
        builder.UseSetting("USE_SYNCFUSION_AGENT_TOOLS", "false");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_dbPath}",
                ["FileStorage:BasePath"] = _storagePath,
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);

            if (Directory.Exists(_storagePath))
                Directory.Delete(_storagePath, recursive: true);
        }

        base.Dispose(disposing);
    }
}
