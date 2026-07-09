using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TIKR.Shared.Interfaces;

namespace TIKR.SyncfusionDocuments;

public static class DependencyInjection
{
    public static IServiceCollection AddSyncfusionDocumentGeneration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        SyncfusionDocumentLicense.RegisterFromConfiguration(configuration);
        return services.AddScoped<IDocumentGenerationService, SyncfusionDocumentGenerationService>();
    }
}
