using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TIKR.Shared.Diagnostics;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>Polls the forward-to-folder inbox when configured.</summary>
public sealed class FolderEmailIngestionHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<FolderEmailIngestionHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TikrActionLog.Info(logger, "Host.EmailIngest", "Poller started");
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("Email folder ingest host cancelled during startup delay");
            return;
        }

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
                    TikrActionLog.Completed(logger, "Host.EmailIngest.Cycle",
                        $"Ingested={result.Ingested} Skipped={result.Skipped} Errors={result.Errors.Count}");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                TikrActionLog.Failed(logger, "Host.EmailIngest.Cycle", ex);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        TikrActionLog.Info(logger, "Host.EmailIngest", "Poller stopped");
    }
}
