using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>Polls the forward-to-folder inbox when configured.</summary>
public sealed class FolderEmailIngestionHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<FolderEmailIngestionHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var ingestion = scope.ServiceProvider.GetRequiredService<IEmailIngestionService>();
                if (!ingestion.IsConfigured)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    continue;
                }

                var result = await ingestion.IngestPendingAsync(stoppingToken);
                if (result.Ingested > 0 || result.Errors.Count > 0)
                {
                    logger.LogInformation(
                        "Email folder ingest: ingested={Ingested}, skipped={Skipped}, errors={ErrorCount}",
                        result.Ingested, result.Skipped, result.Errors.Count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Email folder ingest poll failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}
