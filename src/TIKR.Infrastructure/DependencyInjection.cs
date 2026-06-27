using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TIKR.Infrastructure.Data;
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

        services.AddScoped<IAuditService, AuditService>();
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IHybridAiService, HybridAiService>();
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
    }
}
