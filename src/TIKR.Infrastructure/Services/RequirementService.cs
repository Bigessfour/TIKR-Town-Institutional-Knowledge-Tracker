using TIKR.Infrastructure.Data;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Helpers;
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
            SubmitTo = NormalizeOptional(request.SubmitTo),
            ContactName = NormalizeOptional(request.ContactName),
            ContactEmail = NormalizeOptional(request.ContactEmail),
            ContactPhone = NormalizeOptional(request.ContactPhone),
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

        var details = AuditChangeBuilder.Build(
            entity.Title,
            ("Title", entity.Title, request.Title),
            ("Description", entity.Description, request.Description),
            ("DueDate", entity.DueDate, request.DueDate),
            ("Recurrence", entity.Recurrence, request.Recurrence),
            ("Category", entity.Category, request.Category),
            ("IsCompleted", entity.IsCompleted, request.IsCompleted),
            ("SubmitTo", entity.SubmitTo, request.SubmitTo),
            ("ContactName", entity.ContactName, request.ContactName),
            ("ContactEmail", entity.ContactEmail, request.ContactEmail),
            ("ContactPhone", entity.ContactPhone, request.ContactPhone));

        entity.Title = request.Title;
        entity.Description = request.Description;
        entity.DueDate = request.DueDate;
        entity.Recurrence = request.Recurrence;
        entity.Category = request.Category;
        entity.IsCompleted = request.IsCompleted;
        entity.SubmitTo = NormalizeOptional(request.SubmitTo);
        entity.ContactName = NormalizeOptional(request.ContactName);
        entity.ContactEmail = NormalizeOptional(request.ContactEmail);
        entity.ContactPhone = NormalizeOptional(request.ContactPhone);
        entity.UpdatedAt = DateTime.UtcNow;

        using var tx = await db.Database.BeginTransactionAsync(ct);
        await audit.LogAsync("Update", nameof(Requirement), entity.Id, details, currentUser.UserId, ct);
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

    public async Task LinkDocumentAsync(Guid requirementId, Guid documentId, IAuditService audit, ICurrentUserService currentUser, CancellationToken ct = default)
    {
        var requirement = await db.Requirements.FindAsync(requirementId, ct);
        if (requirement is null) throw new KeyNotFoundException($"Requirement {requirementId} not found.");

        var document = await db.Documents.FindAsync(documentId, ct);
        if (document is null) throw new KeyNotFoundException("Document not found.");

        var existing = await db.RequirementDocuments.FindAsync(new object[] { requirementId, documentId }, ct);
        if (existing is null)
        {
            using var tx = await db.Database.BeginTransactionAsync(ct);
            db.RequirementDocuments.Add(new RequirementDocument
            {
                RequirementId = requirementId,
                DocumentId = documentId,
                LinkedAt = DateTime.UtcNow
            });
            await audit.LogAsync("Link", nameof(Requirement), requirementId, document.FileName, currentUser.UserId, ct);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
    }

    public async Task UnlinkDocumentAsync(Guid requirementId, Guid documentId, IAuditService audit, ICurrentUserService currentUser, CancellationToken ct = default)
    {
        var link = await db.RequirementDocuments.FindAsync(new object[] { requirementId, documentId }, ct);
        if (link is null) throw new KeyNotFoundException($"Link not found for requirement {requirementId} and document {documentId}.");

        using var tx = await db.Database.BeginTransactionAsync(ct);
        db.RequirementDocuments.Remove(link);
        await audit.LogAsync("Unlink", nameof(Requirement), requirementId, documentId.ToString(), currentUser.UserId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
