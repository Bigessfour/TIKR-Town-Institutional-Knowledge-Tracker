using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TIKR.Infrastructure.Data;
using TIKR.Infrastructure.Identity;
using TIKR.Infrastructure.Services;
using TIKR.Shared.Configuration;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTikrInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = TikrConfiguration.GetDatabaseProvider(configuration);

        services.AddDbContext<TikrDbContext>(options =>
        {
            if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(configuration.GetConnectionString("Default")
                    ?? throw new InvalidOperationException("Postgres connection string required."));
            }
            else
            {
                var connectionString = configuration.GetConnectionString("Default") ?? "Data Source=tikr.db";
                options.UseSqlite(connectionString);
            }
        });

        services.AddTikrIdentity(configuration);

        services.AddScoped<IAuditService, AuditService>();
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IHybridAiService, HybridAiService>();
        services.AddScoped<IDocumentAgentService, DocumentAgentService>();
        services.AddHttpClient<GrokService>();

        services.AddSingleton<IOllamaChatClientFactory>(_ =>
            new OllamaChatClientFactory(
                TikrConfiguration.GetOllamaHost(configuration),
                TikrConfiguration.GetChatModel(configuration)));

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TikrDbContext>();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        if (TikrConfiguration.IsAuthEnabled(configuration))
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");
            await IdentitySeeder.SeedAsync(db, userManager, roleManager, configuration, logger);
        }
    }
}
