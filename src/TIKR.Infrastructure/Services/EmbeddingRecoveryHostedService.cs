using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TIKR.Shared.Configuration;
using TIKR.Shared.Diagnostics;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// Production recovery: when Ollama returns or coverage is incomplete, automatically
/// reindex document/vault embeddings with cooldown so Assistant memory heals without
/// Deb needing to open Settings.
/// </summary>
public sealed class EmbeddingRecoveryHostedService(
    IServiceScopeFactory scopeFactory,
    EmbeddingRecoveryState state,
    IConfiguration configuration,
    ILogger<EmbeddingRecoveryHostedService> logger) : BackgroundService
{
    /// <summary>True until we observe a healthy Ollama poll (or after a failed poll).</summary>
    private bool _ollamaWasUnhealthy = true;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = TikrConfiguration.GetEmbeddingRecoveryIntervalSeconds(configuration);
        if (intervalSeconds <= 0)
        {
            logger.LogInformation(
                "Embedding recovery host disabled (TIKR_EMBEDDING_RECOVERY_INTERVAL_SECONDS=0)");
            return;
        }

        var cooldown = TimeSpan.FromMinutes(
            TikrConfiguration.GetEmbeddingRecoveryCooldownMinutes(configuration));

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            return;
        }

        TikrActionLog.Info(logger, "AI.EmbeddingRecovery",
            $"Host started IntervalSec={intervalSeconds} CooldownMin={cooldown.TotalMinutes:0}");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(cooldown, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                state.NoteError(ex.Message);
                logger.LogWarning(ex, "Embedding recovery tick failed");
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
    }

    private async Task TickAsync(TimeSpan cooldown, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var ai = scope.ServiceProvider.GetRequiredService<IHybridAiService>();
        var utcNow = DateTime.UtcNow;

        var status = await ai.GetStatusAsync(ct);
        var ollamaUp = status.OllamaAvailable;
        state.NoteOllama(ollamaUp, utcNow);

        if (!ollamaUp)
        {
            _ollamaWasUnhealthy = true;
            TikrActionLog.Info(logger, "AI.EmbeddingRecovery", "Ollama unavailable — waiting");
            return;
        }

        var recovered = _ollamaWasUnhealthy;
        _ollamaWasUnhealthy = false;

        var health = await ai.GetCorpusHealthAsync(ct);
        state.NoteCorpus(health);

        var needs = EmbeddingRecoveryState.NeedsRecovery(health);
        if (!needs)
        {
            if (recovered)
            {
                TikrActionLog.Info(logger, "AI.EmbeddingRecovery",
                    "Ollama healthy; corpus already complete — no reindex");
            }
            return;
        }

        if (state.LastAutoReindexUtc is { } last && utcNow - last < cooldown)
        {
            TikrActionLog.Info(logger, "AI.EmbeddingRecovery",
                $"Cooldown active until {last.Add(cooldown):u} — skip " +
                $"(docs={health.DocumentsChunkCoveragePercent}% vault={health.KnowledgeChunkCoveragePercent}%)");
            return;
        }

        var trigger = recovered ? "auto-recovery-ollama" : "auto-recovery-coverage";
        TikrActionLog.Started(logger, "AI.EmbeddingRecovery",
            $"Trigger={trigger} DocCoverage={health.DocumentsChunkCoveragePercent}% " +
            $"VaultCoverage={health.KnowledgeChunkCoveragePercent}%");

        var result = await ai.ReindexAllEmbeddingsAsync(trigger, ct);
        state.NoteReindexResult(trigger, result, utcNow);

        var after = await ai.GetCorpusHealthAsync(ct);
        state.NoteCorpus(after);

        TikrActionLog.Completed(logger, "AI.EmbeddingRecovery",
            $"Trigger={trigger} Docs={result.DocumentsEmbedded}/{result.DocumentsAttempted} " +
            $"Vault={result.KnowledgeEmbedded}/{result.KnowledgeAttempted} Errors={result.Errors.Count} " +
            $"DocCoverageNow={after.DocumentsChunkCoveragePercent}%");
    }
}
