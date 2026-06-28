using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TIKR.Infrastructure;
using TIKR.Infrastructure.Data;
using TIKR.Infrastructure.Identity;
using TIKR.Shared.Configuration;
using TIKR.Shared.TestFixtures;

namespace TIKR.Infrastructure.Tests.Identity;

public class IdentitySeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesAdmin_WhenAuthEnabledAndNoUsers()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"tikr-seed-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";

        try
        {
            var options = new DbContextOptionsBuilder<TikrDbContext>()
                .UseSqlite(connectionString)
                .Options;

            await using var db = new TikrDbContext(options);
            await db.Database.EnsureCreatedAsync();

            var config = BuildConfig(new Dictionary<string, string?>
            {
                ["TIKR_ADMIN_EMAIL"] = "seed-admin@test.gov",
                ["TIKR_ADMIN_PASSWORD"] = TestAuthFixtures.BootstrapPassword,
                ["TIKR_JWT_SIGNING_KEY"] = TestAuthFixtures.JwtSigningKey
            });

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<TikrDbContext>(o => o.UseSqlite(connectionString));
            services.AddTikrIdentity(config);
            var provider = services.BuildServiceProvider();

            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = NullLoggerFactory.Instance.CreateLogger("test");

            await IdentitySeeder.SeedAsync(db, userManager, roleManager, config, logger);

            var admin = await userManager.FindByEmailAsync("seed-admin@test.gov");
            admin.Should().NotBeNull();
            (await userManager.IsInRoleAsync(admin!, "Admin")).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }

    [Fact]
    public async Task SeedAsync_Skips_WhenAuthDisabled()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"tikr-seed-off-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";

        try
        {
            var options = new DbContextOptionsBuilder<TikrDbContext>()
                .UseSqlite(connectionString)
                .Options;

            await using var db = new TikrDbContext(options);
            await db.Database.EnsureCreatedAsync();

            var config = BuildConfig(new Dictionary<string, string?> { ["TIKR_AUTH_ENABLED"] = "false" });
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDbContext<TikrDbContext>(o => o.UseSqlite(connectionString));
            services.AddTikrIdentity(config);
            var provider = services.BuildServiceProvider();

            var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = NullLoggerFactory.Instance.CreateLogger("test");

            await IdentitySeeder.SeedAsync(db, userManager, roleManager, config, logger);
            (await userManager.Users.CountAsync()).Should().Be(0);
        }
        finally
        {
            if (File.Exists(dbPath))
                File.Delete(dbPath);
        }
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
