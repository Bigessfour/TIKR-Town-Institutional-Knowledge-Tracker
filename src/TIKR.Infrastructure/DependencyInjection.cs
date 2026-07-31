using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TIKR.Infrastructure.Data;
using TIKR.Infrastructure.Identity;
using TIKR.Infrastructure.Services;
using TIKR.SyncfusionDocuments;
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

        // Shared Data Protection keys so clerk secrets encrypt/decrypt across restarts (NAS volume).
        var dp = services.AddDataProtection().SetApplicationName("TIKR");
        var dpPath = configuration["TIKR_DATA_PROTECTION_PATH"];
        if (string.IsNullOrWhiteSpace(dpPath) && Directory.Exists("/data"))
            dpPath = "/data/.dpkeys";
        if (!string.IsNullOrWhiteSpace(dpPath))
        {
            try
            {
                Directory.CreateDirectory(dpPath);
                dp.PersistKeysToFileSystem(new DirectoryInfo(dpPath));
            }
            catch
            {
                // Fall back to ephemeral keys (dev only).
            }
        }

        services.AddTikrIdentity(configuration);

        services.AddSingleton<ISecretProtector, SecretProtector>();
        services.AddSingleton<IRuntimeSecretsStore, RuntimeSecretsStore>();
        services.AddSingleton<FeatureSettingsState>();
        services.AddScoped<IFeatureSettingsService, FeatureSettingsService>();

        services.AddScoped<IAuditService, AuditService>();
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IHybridAiService, HybridAiService>();
        services.AddScoped<IAgentDocumentStorage, NasAgentDocumentStorage>();
        services.AddSingleton<NasSyncfusionDocumentStorage>();
        services.AddSingleton<SyncfusionDocumentAgentToolRegistry>();
        services.AddScoped<SyncfusionDocumentAgentOrchestrator>();
        services.AddScoped<SyncfusionDocumentAgentExtractor>();
        services.AddScoped<StubDocumentAgentExtractionBackend>();
        services.AddScoped<SyncfusionDocumentAgentExtractionBackend>();
        services.AddScoped<IDocumentAgentExtractionBackend>(sp =>
        {
            var state = sp.GetRequiredService<FeatureSettingsState>();
            return state.Current.UseSyncfusionAgentTools
                ? sp.GetRequiredService<SyncfusionDocumentAgentExtractionBackend>()
                : sp.GetRequiredService<StubDocumentAgentExtractionBackend>();
        });
        services.AddScoped<IDocumentAgentService, DocumentAgentService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IRequirementService, RequirementService>();
        services.AddScoped<IKnowledgeService, KnowledgeService>();
        services.AddScoped<IChatHistoryService, ChatHistoryService>();
        services.AddSyncfusionDocumentGeneration(configuration);
        services.AddScoped<ICouncilPacketService, CouncilPacketService>();
        services.AddHttpClient<GrokService>();
        services.AddSingleton<IEmailIngestionService, FolderEmailIngestionService>();
        services.AddHostedService<FolderEmailIngestionHostedService>();
        services.AddSingleton<ILibraryScanService, LibraryScanService>();
        services.AddHostedService<LibraryScanHostedService>();
        services.AddSingleton<EmbeddingRecoveryState>();
        services.AddHostedService<EmbeddingRecoveryHostedService>();
        services.AddSingleton<TownDocumentSearchToolRegistry>();

        services.AddSingleton<IOllamaChatClientFactory>(sp =>
            new OllamaChatClientFactory(sp.GetRequiredService<FeatureSettingsState>()));

        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TikrDbContext>();
        await db.Database.MigrateAsync();
        await DbSeeder.SeedAsync(db);

        var featureSettings = scope.ServiceProvider.GetRequiredService<IFeatureSettingsService>();
        await featureSettings.LoadIntoStateAsync();

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
