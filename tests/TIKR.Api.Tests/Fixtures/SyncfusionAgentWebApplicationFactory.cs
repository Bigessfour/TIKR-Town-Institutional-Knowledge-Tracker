using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TIKR.Api.Tests.Fixtures;

/// <summary>
/// API test host with Syncfusion Document SDK agent tools enabled.
/// Requires <c>SYNCFUSION_LICENSE_KEY</c> for PDF/DOCX extraction assertions.
/// </summary>
public class SyncfusionAgentWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"tikr-sf-api-{Guid.NewGuid():N}.db");
    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), $"tikr-sf-api-storage-{Guid.NewGuid():N}");

    internal static bool IsLicensed =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("TIKR_AUTH_ENABLED", "false");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_dbPath}",
                ["FileStorage:BasePath"] = _storagePath,
                ["USE_GROK"] = "false",
                ["OLLAMA_HOST"] = "http://127.0.0.1:1",
                ["TIKR_AUTH_ENABLED"] = "false",
                ["USE_SYNCFUSION_AGENT_TOOLS"] = "true",
                ["USE_SYNCFUSION_AGENT_ORCHESTRATION"] = "false",
                ["SYNCFUSION_LICENSE_KEY"] = Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY")
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
