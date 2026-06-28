using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

using TIKR.Shared.TestFixtures;

namespace TIKR.Api.Tests.Fixtures;

public class AuthEnabledWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = TestAuthFixtures.AdminEmail;
    public const string AdminPassword = TestAuthFixtures.BootstrapPassword;
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"tikr-auth-api-{Guid.NewGuid():N}.db");
    private readonly string _storagePath = Path.Combine(Path.GetTempPath(), $"tikr-auth-storage-{Guid.NewGuid():N}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("TIKR_ADMIN_EMAIL", AdminEmail);
        builder.UseSetting("TIKR_ADMIN_PASSWORD", AdminPassword);
        builder.UseSetting("TIKR_JWT_SIGNING_KEY", TestAuthConstants.JwtSigningKey);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_dbPath}",
                ["FileStorage:BasePath"] = _storagePath,
                ["USE_GROK"] = "false",
                ["OLLAMA_HOST"] = "http://127.0.0.1:1",
                ["TIKR_ADMIN_EMAIL"] = AdminEmail,
                ["TIKR_ADMIN_PASSWORD"] = AdminPassword,
                ["TIKR_JWT_SIGNING_KEY"] = TestAuthConstants.JwtSigningKey
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
