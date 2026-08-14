using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TIKR.Api.Tests.Fixtures;

public class EmailInboxWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"tikr-email-api-{Guid.NewGuid():N}.db");
    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), $"tikr-email-api-storage-{Guid.NewGuid():N}");
    public string InboxPath { get; } = Path.Combine(Path.GetTempPath(), $"tikr-email-inbox-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(InboxPath);
        Directory.CreateDirectory(_storagePath);

        builder.UseEnvironment("Testing");
        builder.UseSetting("TIKR_AUTH_ENABLED", "false");
        builder.UseSetting("SYNCFUSION_LICENSE_KEY", "");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_dbPath}",
                ["FileStorage:BasePath"] = _storagePath,
                ["FILE_STORAGE_PATH"] = _storagePath,
                ["USE_GROK"] = "false",
                ["OLLAMA_HOST"] = "http://127.0.0.1:1",
                ["TIKR_AUTH_ENABLED"] = "false",
                ["USE_SYNCFUSION_AGENT_TOOLS"] = "false",
                ["SYNCFUSION_LICENSE_KEY"] = "",
                ["TIKR_EMAIL_INBOX_PATH"] = InboxPath,
                ["TIKR_EMAIL_STRUCTURED_EXTRACT"] = "true"
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
            if (Directory.Exists(InboxPath))
                Directory.Delete(InboxPath, recursive: true);
        }

        base.Dispose(disposing);
    }
}
