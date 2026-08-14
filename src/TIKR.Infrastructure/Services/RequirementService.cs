using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Diagnostics;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// Requirement business logic (CRUD centralization). Minimal initial impl for cleanup; expand as needed.
/// </summary>
public class RequirementService(TikrDbContext db, ILogger<RequirementService>? logger = null) : IRequirementService
{
    private readonly ILogger _log = logger ?? NullLogger<RequirementService>.Instance;

    public async Task<Requirement> CreateAsync(
        CreateRequirementRequest request,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        TikrActionLog.Started(_log, "Requirement.Create", $"Title={request.Title} Due={request.DueDate}");

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

        TikrActionLog.Completed(_log, "Requirement.Create", $"RequirementId={entity.Id} Title={entity.Title}");
        return entity;
    }

    public async Task<Requirement> UpdateAsync(Guid id, UpdateRequirementRequest request, IAuditService audit, ICurrentUserService currentUser, CancellationToken ct = default)
    {
        TikrActionLog.Started(_log, "Requirement.Update", $"RequirementId={id} Title={request.Title} Completed={request.IsCompleted}");

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

        TikrActionLog.Completed(_log, "Requirement.Update", $"RequirementId={entity.Id} Title={entity.Title}");
        return entity;
    }

    public async Task DeleteAsync(Guid id, IAuditService audit, ICurrentUserService currentUser, CancellationToken ct = default)
    {
        TikrActionLog.Started(_log, "Requirement.Delete", $"RequirementId={id}");

        var entity = await db.Requirements.FindAsync(id);
        if (entity is null) throw new KeyNotFoundException($"Requirement {id} not found.");
        if (entity.IsSystemSeeded) throw new InvalidOperationException("System-seeded requirements cannot be deleted.");

        using var tx = await db.Database.BeginTransactionAsync(ct);
        db.Requirements.Remove(entity);
        await audit.LogAsync("Delete", nameof(Requirement), id, entity.Title, currentUser.UserId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        TikrActionLog.Completed(_log, "Requirement.Delete", $"RequirementId={id} Title={entity.Title}");
    }

    public async Task LinkDocumentAsync(Guid requirementId, Guid documentId, IAuditService audit, ICurrentUserService currentUser, CancellationToken ct = default)
    {
        TikrActionLog.Started(_log, "Requirement.LinkDocument",
            $"RequirementId={requirementId} DocumentId={documentId}");

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
            TikrActionLog.Completed(_log, "Requirement.LinkDocument",
                $"RequirementId={requirementId} FileName={document.FileName}");
        }
        else
        {
            TikrActionLog.Info(_log, "Requirement.LinkDocument", "Link already existed (no-op)");
        }
    }

    public async Task UnlinkDocumentAsync(Guid requirementId, Guid documentId, IAuditService audit, ICurrentUserService currentUser, CancellationToken ct = default)
    {
        TikrActionLog.Started(_log, "Requirement.UnlinkDocument",
            $"RequirementId={requirementId} DocumentId={documentId}");

        var link = await db.RequirementDocuments.FindAsync(new object[] { requirementId, documentId }, ct);
        if (link is null) throw new KeyNotFoundException($"Link not found for requirement {requirementId} and document {documentId}.");

        using var tx = await db.Database.BeginTransactionAsync(ct);
        db.RequirementDocuments.Remove(link);
        await audit.LogAsync("Unlink", nameof(Requirement), requirementId, documentId.ToString(), currentUser.UserId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        TikrActionLog.Completed(_log, "Requirement.UnlinkDocument",
            $"RequirementId={requirementId} DocumentId={documentId}");
    }

    public async Task<IReadOnlyList<RequirementChecklistItem>> ListChecklistAsync(
        Guid requirementId,
        CancellationToken ct = default) =>
        await db.RequirementChecklistItems
            .AsNoTracking()
            .Where(i => i.RequirementId == requirementId)
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Title)
            .ToListAsync(ct);

    public async Task<RequirementChecklistItem> AddChecklistItemAsync(
        Guid requirementId,
        CreateRequirementChecklistItemRequest request,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        TikrActionLog.Started(_log, "Requirement.Checklist.Create",
            $"RequirementId={requirementId} Title={request.Title} UserId={currentUser.UserId}");

        var requirement = await db.Requirements.FindAsync([requirementId], ct)
                          ?? throw new KeyNotFoundException($"Requirement {requirementId} not found.");

        await ValidateOptionalLinksAsync(request.LinkedDocumentId, request.ContactId, ct);

        var maxSort = await db.RequirementChecklistItems
            .Where(i => i.RequirementId == requirementId)
            .Select(i => (int?)i.SortOrder)
            .MaxAsync(ct) ?? -1;

        var entity = new RequirementChecklistItem
        {
            Id = Guid.NewGuid(),
            RequirementId = requirementId,
            Title = request.Title.Trim(),
            Description = NormalizeOptional(request.Description),
            IsRequired = request.IsRequired,
            IsCompleted = false,
            DueOffsetDays = request.DueOffsetDays,
            DueDate = request.DueDate,
            SortOrder = request.SortOrder ?? maxSort + 1,
            LinkedDocumentId = request.LinkedDocumentId,
            DocumentTemplateHint = NormalizeOptional(request.DocumentTemplateHint),
            SubmitTo = NormalizeOptional(request.SubmitTo),
            ContactId = request.ContactId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        using var tx = await db.Database.BeginTransactionAsync(ct);
        db.RequirementChecklistItems.Add(entity);
        requirement.UpdatedAt = DateTime.UtcNow;
        await audit.LogAsync("ChecklistCreate", nameof(Requirement), requirementId, entity.Title, currentUser.UserId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        TikrActionLog.Completed(_log, "Requirement.Checklist.Create",
            $"ChecklistItemId={entity.Id} RequirementId={requirementId}");
        return entity;
    }

    public async Task<RequirementChecklistItem> UpdateChecklistItemAsync(
        Guid requirementId,
        Guid itemId,
        UpdateRequirementChecklistItemRequest request,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        TikrActionLog.Started(_log, "Requirement.Checklist.Update",
            $"RequirementId={requirementId} ChecklistItemId={itemId} UserId={currentUser.UserId}");

        var entity = await FindChecklistItemAsync(requirementId, itemId, ct);
        await ValidateOptionalLinksAsync(request.LinkedDocumentId, request.ContactId, ct);

        var details = AuditChangeBuilder.Build(
            entity.Title,
            ("Title", entity.Title, request.Title),
            ("IsCompleted", entity.IsCompleted, request.IsCompleted),
            ("SortOrder", entity.SortOrder, request.SortOrder),
            ("DueOffsetDays", entity.DueOffsetDays, request.DueOffsetDays),
            ("DueDate", entity.DueDate, request.DueDate));

        entity.Title = request.Title.Trim();
        entity.Description = NormalizeOptional(request.Description);
        entity.IsRequired = request.IsRequired;
        entity.IsCompleted = request.IsCompleted;
        entity.DueOffsetDays = request.DueOffsetDays;
        entity.DueDate = request.DueDate;
        entity.SortOrder = request.SortOrder;
        entity.LinkedDocumentId = request.LinkedDocumentId;
        entity.DocumentTemplateHint = NormalizeOptional(request.DocumentTemplateHint);
        entity.SubmitTo = NormalizeOptional(request.SubmitTo);
        entity.ContactId = request.ContactId;
        entity.UpdatedAt = DateTime.UtcNow;

        using var tx = await db.Database.BeginTransactionAsync(ct);
        await audit.LogAsync("ChecklistUpdate", nameof(Requirement), requirementId, details, currentUser.UserId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        TikrActionLog.Completed(_log, "Requirement.Checklist.Update",
            $"ChecklistItemId={itemId} Completed={entity.IsCompleted}");
        return entity;
    }

    public async Task CompleteChecklistItemAsync(
        Guid requirementId,
        Guid itemId,
        bool isCompleted,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        TikrActionLog.Started(_log, "Requirement.Checklist.Complete",
            $"RequirementId={requirementId} ChecklistItemId={itemId} IsCompleted={isCompleted} UserId={currentUser.UserId}");

        var entity = await FindChecklistItemAsync(requirementId, itemId, ct);
        entity.IsCompleted = isCompleted;
        entity.UpdatedAt = DateTime.UtcNow;

        using var tx = await db.Database.BeginTransactionAsync(ct);
        await audit.LogAsync(
            isCompleted ? "ChecklistComplete" : "ChecklistUncomplete",
            nameof(Requirement),
            requirementId,
            entity.Title,
            currentUser.UserId,
            ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        TikrActionLog.Completed(_log, "Requirement.Checklist.Complete",
            $"ChecklistItemId={itemId} IsCompleted={isCompleted}");
    }

    public async Task DeleteChecklistItemAsync(
        Guid requirementId,
        Guid itemId,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        TikrActionLog.Started(_log, "Requirement.Checklist.Delete",
            $"RequirementId={requirementId} ChecklistItemId={itemId} UserId={currentUser.UserId}");

        var entity = await FindChecklistItemAsync(requirementId, itemId, ct);
        using var tx = await db.Database.BeginTransactionAsync(ct);
        db.RequirementChecklistItems.Remove(entity);
        await audit.LogAsync("ChecklistDelete", nameof(Requirement), requirementId, entity.Title, currentUser.UserId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        TikrActionLog.Completed(_log, "Requirement.Checklist.Delete", $"ChecklistItemId={itemId}");
    }

    public async Task ReorderChecklistAsync(
        Guid requirementId,
        IReadOnlyList<Guid> orderedIds,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        TikrActionLog.Started(_log, "Requirement.Checklist.Reorder",
            $"RequirementId={requirementId} Count={orderedIds.Count} UserId={currentUser.UserId}");

        _ = await db.Requirements.FindAsync([requirementId], ct)
            ?? throw new KeyNotFoundException($"Requirement {requirementId} not found.");

        var items = await db.RequirementChecklistItems
            .Where(i => i.RequirementId == requirementId)
            .ToListAsync(ct);

        if (orderedIds.Count != items.Count || orderedIds.Any(id => items.All(i => i.Id != id)))
            throw new InvalidOperationException("Reorder list must include every checklist item exactly once.");

        for (var i = 0; i < orderedIds.Count; i++)
        {
            var item = items.First(x => x.Id == orderedIds[i]);
            item.SortOrder = i;
            item.UpdatedAt = DateTime.UtcNow;
        }

        using var tx = await db.Database.BeginTransactionAsync(ct);
        await audit.LogAsync("ChecklistReorder", nameof(Requirement), requirementId, $"Count={orderedIds.Count}", currentUser.UserId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        TikrActionLog.Completed(_log, "Requirement.Checklist.Reorder", $"RequirementId={requirementId}");
    }

    private async Task<RequirementChecklistItem> FindChecklistItemAsync(Guid requirementId, Guid itemId, CancellationToken ct)
    {
        var entity = await db.RequirementChecklistItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.RequirementId == requirementId, ct);
        return entity ?? throw new KeyNotFoundException($"Checklist item {itemId} not found on requirement {requirementId}.");
    }

    private async Task ValidateOptionalLinksAsync(Guid? documentId, Guid? contactId, CancellationToken ct)
    {
        if (documentId is { } docId && await db.Documents.FindAsync([docId], ct) is null)
            throw new KeyNotFoundException($"Document {docId} not found.");
        if (contactId is { } cId && await db.Contacts.FindAsync([cId], ct) is null)
            throw new KeyNotFoundException($"Contact {cId} not found.");
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
