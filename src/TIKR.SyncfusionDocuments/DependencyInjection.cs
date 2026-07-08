using Microsoft.Extensions.DependencyInjection;
using TIKR.Shared.Interfaces;

namespace TIKR.SyncfusionDocuments;

public static class DependencyInjection
{
    public static IServiceCollection AddSyncfusionDocumentGeneration(this IServiceCollection services) =>
        services.AddScoped<IDocumentGenerationService, SyncfusionDocumentGenerationService>();
}
