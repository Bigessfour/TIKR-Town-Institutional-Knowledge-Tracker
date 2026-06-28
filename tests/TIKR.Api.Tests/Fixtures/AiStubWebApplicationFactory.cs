using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TIKR.Shared.Interfaces;

namespace TIKR.Api.Tests.Fixtures;

/// <summary>
/// Test factory that replaces <see cref="IHybridAiService"/> with <see cref="StubHybridAiService"/>.
/// </summary>
public sealed class AiStubWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"tikr-api-stub-{Guid.NewGuid():N}.db");
    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), $"tikr-api-stub-storage-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_dbPath}",
                ["FileStorage:BasePath"] = _storagePath,
                ["USE_GROK"] = "true",
                ["GROK_API_KEY"] = "stub-key"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IHybridAiService>();
            services.AddSingleton<IHybridAiService, StubHybridAiService>();
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
