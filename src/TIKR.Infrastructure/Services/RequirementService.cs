using TIKR.Infrastructure.Data;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// Requirement business logic (CRUD centralization). Minimal initial impl for cleanup; expand as needed.
/// </summary>
public class RequirementService(TikrDbContext db) : IRequirementService
{
    public async Task<Requirement> CreateAsync(
        CreateRequirementRequest request,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        var entity = new Requirement
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            Recurrence = request.Recurrence,
            Category = request.Category,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        using var tx = await db.Database.BeginTransactionAsync(ct);
        db.Requirements.Add(entity);
        await audit.LogAsync("Create", nameof(Requirement), entity.Id, entity.Title, currentUser.UserId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return entity;
    }

    public async Task<Requirement> UpdateAsync(Guid id, UpdateRequirementRequest request, IAuditService audit, ICurrentUserService currentUser, CancellationToken ct = default)
    {
        var entity = await db.Requirements.FindAsync(id);
        if (entity is null) throw new KeyNotFoundException($"Requirement {id} not found.");

        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.DueDate = request.DueDate;
        entity.Recurrence = request.Recurrence;
        entity.Category = request.Category;
        entity.IsCompleted = request.IsCompleted;
        entity.UpdatedAt = DateTime.UtcNow;

        using var tx = await db.Database.BeginTransactionAsync(ct);
        await audit.LogAsync("Update", nameof(Requirement), entity.Id, entity.Title, currentUser.UserId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return entity;
    }

    public async Task DeleteAsync(Guid id, IAuditService audit, ICurrentUserService currentUser, CancellationToken ct = default)
    {
        var entity = await db.Requirements.FindAsync(id);
        if (entity is null) throw new KeyNotFoundException($"Requirement {id} not found.");
        if (entity.IsSystemSeeded) throw new InvalidOperationException("System-seeded requirements cannot be deleted.");

        using var tx = await db.Database.BeginTransactionAsync(ct);
        db.Requirements.Remove(entity);
        await audit.LogAsync("Delete", nameof(Requirement), id, entity.Title, currentUser.UserId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
