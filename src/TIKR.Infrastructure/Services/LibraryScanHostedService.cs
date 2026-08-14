using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TIKR.Shared.Diagnostics;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>Polls the NAS library scan path when <c>TIKR_LIBRARY_SCAN_PATH</c> is configured.</summary>
public sealed class LibraryScanHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<LibraryScanHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TikrActionLog.Info(logger, "Host.LibraryScan", "Poller started");
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("Library scan host cancelled during startup delay");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalSeconds = 300;
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var scanner = scope.ServiceProvider.GetRequiredService<ILibraryScanService>();
                intervalSeconds = scanner.IntervalSeconds;

                if (!scanner.IsConfigured)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    continue;
                }

                var result = await scanner.ScanAsync(stoppingToken);
                if (result.Imported > 0 || result.Failed > 0 || result.Errors.Count > 0)
                {
                    TikrActionLog.Completed(logger, "Host.LibraryScan.Cycle",
                        $"Scanned={result.Scanned} Imported={result.Imported} Skipped={result.Skipped} Failed={result.Failed} Errors={result.Errors.Count}");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                TikrActionLog.Failed(logger, "Host.LibraryScan.Cycle", ex);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(30, intervalSeconds)), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        TikrActionLog.Info(logger, "Host.LibraryScan", "Poller stopped");
    }
}
