using TIKR.Infrastructure.Data;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// KnowledgeEntry logic centralization (CRUD + embeds). Minimal for cleanup.
/// </summary>
public class KnowledgeService(TikrDbContext db) : IKnowledgeService
{
    public async Task<KnowledgeEntry> CreateAsync(
        CreateKnowledgeEntryRequest request,
        IAuditService audit,
        IHybridAiService ai,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
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

        return entity;
    }

    public async Task<KnowledgeEntry> UpdateAsync(Guid id, UpdateKnowledgeEntryRequest request, IAuditService audit, IHybridAiService ai, ICurrentUserService currentUser, CancellationToken ct = default)
    {
        var entity = await db.KnowledgeEntries.FindAsync(id);
        if (entity is null) throw new KeyNotFoundException($"Knowledge entry {id} not found.");

        entity.Title = request.Title;
        entity.Content = request.Content;
        entity.Category = request.Category;
        entity.SortOrder = request.SortOrder;
        entity.UpdatedAt = DateTime.UtcNow;

        using var tx = await db.Database.BeginTransactionAsync(ct);
        await audit.LogAsync("Update", nameof(KnowledgeEntry), entity.Id, entity.Title, currentUser.UserId, ct);
        _ = await ai.EmbedKnowledgeEntryAsync(entity.Id, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return entity;
    }

    public async Task DeleteAsync(Guid id, IAuditService audit, ICurrentUserService currentUser, CancellationToken ct = default)
    {
        var entity = await db.KnowledgeEntries.FindAsync(id);
        if (entity is null) throw new KeyNotFoundException($"Knowledge entry {id} not found.");

        using var tx = await db.Database.BeginTransactionAsync(ct);
        db.KnowledgeEntries.Remove(entity);
        await audit.LogAsync("Delete", nameof(KnowledgeEntry), id, entity.Title, currentUser.UserId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
