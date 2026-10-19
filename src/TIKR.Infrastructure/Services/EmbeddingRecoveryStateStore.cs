using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Entities;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// Persists <see cref="EmbeddingRecoveryState"/> to SQLite so cooldown survives API restarts (NAS OOM loop guard).
/// </summary>
public sealed class EmbeddingRecoveryStateStore
{
    public const string AppSettingKey = "system.embeddingRecoveryState";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task LoadIntoAsync(
        EmbeddingRecoveryState state,
        TikrDbContext db,
        CancellationToken cancellationToken = default)
    {
        var row = await db.AppSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == AppSettingKey, cancellationToken);
        if (row is null || string.IsNullOrWhiteSpace(row.Value))
            return;

        try
        {
            var dto = JsonSerializer.Deserialize<EmbeddingRecoveryPersistedDto>(row.Value, JsonOptions);
            if (dto is not null)
                state.ApplyPersisted(dto);
        }
        catch (JsonException)
        {
            // Corrupt row — ignore; operator can clear AppSettings if needed.
        }
    }

    public async Task SaveAsync(
        EmbeddingRecoveryState state,
        TikrDbContext db,
        CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(state.ToPersistedDto(), JsonOptions);
        var row = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == AppSettingKey, cancellationToken);
        if (row is null)
        {
            db.AppSettings.Add(new AppSetting
            {
                Key = AppSettingKey,
                Value = json,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            row.Value = json;
            row.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

internal sealed record EmbeddingRecoveryPersistedDto(
    DateTime? LastReindexAttemptUtc,
    DateTime? LastAutoReindexUtc,
    string? LastTrigger,
    string? LastResultSummary,
    string? LastError,
    double LastDocumentsCoveragePercent,
    double LastKnowledgeCoveragePercent);
