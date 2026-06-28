using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TIKR.Api.Tests.Fixtures;

public class TikrWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"tikr-api-{Guid.NewGuid():N}.db");
    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), $"tikr-api-storage-{Guid.NewGuid():N}");

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
                ["USE_SYNCFUSION_AGENT_TOOLS"] = "false"
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
