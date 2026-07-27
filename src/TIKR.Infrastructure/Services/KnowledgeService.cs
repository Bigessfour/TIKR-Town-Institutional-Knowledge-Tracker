using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Diagnostics;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;
using TIKR.Shared.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// KnowledgeEntry logic centralization (CRUD + embeds). Minimal for cleanup.
/// </summary>
public class KnowledgeService(TikrDbContext db, ILogger<KnowledgeService>? logger = null) : IKnowledgeService
{
    private readonly ILogger _log = logger ?? NullLogger<KnowledgeService>.Instance;

    public async Task<KnowledgeEntry> CreateAsync(
        CreateKnowledgeEntryRequest request,
        IAuditService audit,
        IHybridAiService ai,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        TikrActionLog.Started(_log, "Vault.Create", $"Title={request.Title} Category={request.Category}");

        var entity = new KnowledgeEntry
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Content = request.Content,
            Category = request.Category,
            SortOrder = request.SortOrder,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        using var tx = await db.Database.BeginTransactionAsync(ct);
        db.KnowledgeEntries.Add(entity);
        await audit.LogAsync("Create", nameof(KnowledgeEntry), entity.Id, entity.Title, currentUser.UserId, ct);

        _ = await ai.EmbedKnowledgeEntryAsync(entity.Id, ct);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        TikrActionLog.Completed(_log, "Vault.Create", $"EntryId={entity.Id} Title={entity.Title}");
        return entity;
    }

    public async Task<KnowledgeEntry> UpdateAsync(Guid id, UpdateKnowledgeEntryRequest request, IAuditService audit, IHybridAiService ai, ICurrentUserService currentUser, CancellationToken ct = default)
    {
        TikrActionLog.Started(_log, "Vault.Update", $"EntryId={id} Title={request.Title}");

        var entity = await db.KnowledgeEntries.FindAsync(id);
        if (entity is null) throw new KeyNotFoundException($"Knowledge entry {id} not found.");

        var details = AuditChangeBuilder.Build(
            entity.Title,
            ("Title", entity.Title, request.Title),
            ("Content", entity.Content, request.Content),
            ("Category", entity.Category, request.Category),
            ("SortOrder", entity.SortOrder, request.SortOrder));

        entity.Title = request.Title;
        entity.Content = request.Content;
        entity.Category = request.Category;
        entity.SortOrder = request.SortOrder;
        entity.UpdatedAt = DateTime.UtcNow;

        using var tx = await db.Database.BeginTransactionAsync(ct);
        await audit.LogAsync("Update", nameof(KnowledgeEntry), entity.Id, details, currentUser.UserId, ct);
        _ = await ai.EmbedKnowledgeEntryAsync(entity.Id, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        TikrActionLog.Completed(_log, "Vault.Update", $"EntryId={entity.Id} Title={entity.Title}");
        return entity;
    }

    public async Task DeleteAsync(Guid id, IAuditService audit, ICurrentUserService currentUser, CancellationToken ct = default)
    {
        TikrActionLog.Started(_log, "Vault.Delete", $"EntryId={id}");

        var entity = await db.KnowledgeEntries.FindAsync(id);
        if (entity is null) throw new KeyNotFoundException($"Knowledge entry {id} not found.");

        using var tx = await db.Database.BeginTransactionAsync(ct);
        var chunks = await db.EmbeddingChunks
            .Where(c => c.SourceType == EmbeddingSourceType.Knowledge && c.SourceId == id)
            .ToListAsync(ct);
        if (chunks.Count > 0)
            db.EmbeddingChunks.RemoveRange(chunks);
        db.KnowledgeEntries.Remove(entity);
        await audit.LogAsync("Delete", nameof(KnowledgeEntry), id, entity.Title, currentUser.UserId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        TikrActionLog.Completed(_log, "Vault.Delete", $"EntryId={id} Title={entity.Title} ChunksRemoved={chunks.Count}");
    }
}
