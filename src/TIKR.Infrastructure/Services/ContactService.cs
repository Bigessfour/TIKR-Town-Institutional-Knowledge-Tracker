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

public class ContactService(TikrDbContext db, ILogger<ContactService>? logger = null) : IContactService
{
    private readonly ILogger _log = logger ?? NullLogger<ContactService>.Instance;

    public async Task<IReadOnlyList<Contact>> ListAsync(
        string? query = null,
        ContactCategory? category = null,
        bool deleted = false,
        CancellationToken ct = default)
    {
        var q = deleted
            ? db.Contacts.AsNoTracking().Where(c => c.DeletedAt != null)
            : db.Contacts.AsNoTracking().Where(c => c.DeletedAt == null);

        if (category is { } cat && cat != ContactCategory.None)
            q = q.Where(c => (c.Categories & cat) != 0);

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(c =>
                c.Name.Contains(term) ||
                (c.Role != null && c.Role.Contains(term)) ||
                (c.Organization != null && c.Organization.Contains(term)) ||
                (c.Email != null && c.Email.Contains(term)) ||
                (c.Phone != null && c.Phone.Contains(term)));
        }

        return await q.OrderBy(c => c.Name).ToListAsync(ct);
    }

    public async Task<Contact?> GetAsync(Guid id, CancellationToken ct = default) =>
        await db.Contacts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Contact> CreateAsync(
        CreateContactRequest request,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        TikrActionLog.Started(_log, "Contact.Create",
            $"Name={request.Name} Category={request.Categories} UserId={currentUser.UserId}");

        var entity = new Contact
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Role = NormalizeOptional(request.Role),
            Organization = NormalizeOptional(request.Organization),
            Address = NormalizeOptional(request.Address),
            Office = NormalizeOptional(request.Office),
            Email = NormalizeOptional(request.Email),
            Phone = NormalizeOptional(request.Phone),
            Notes = NormalizeOptional(request.Notes),
            Categories = request.Categories == ContactCategory.None ? ContactCategory.Custom : request.Categories,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = currentUser.UserId
        };

        using var tx = await db.Database.BeginTransactionAsync(ct);
        db.Contacts.Add(entity);
        await audit.LogAsync("Create", nameof(Contact), entity.Id, entity.Name, currentUser.UserId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        TikrActionLog.Completed(_log, "Contact.Create",
            $"ContactId={entity.Id} Category={entity.Categories} UserId={currentUser.UserId}");
        return entity;
    }

    public async Task<Contact> UpdateAsync(
        Guid id,
        UpdateContactRequest request,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        TikrActionLog.Started(_log, "Contact.Update",
            $"ContactId={id} Category={request.Categories} UserId={currentUser.UserId}");

        var entity = await db.Contacts.FindAsync([id], ct)
                     ?? throw new KeyNotFoundException($"Contact {id} not found.");
        if (entity.DeletedAt is not null)
            throw new InvalidOperationException("Cannot update a deleted contact. Restore it first.");

        var details = AuditChangeBuilder.Build(
            entity.Name,
            ("Name", entity.Name, request.Name),
            ("Role", entity.Role, request.Role),
            ("Organization", entity.Organization, request.Organization),
            ("Address", entity.Address, request.Address),
            ("Office", entity.Office, request.Office),
            ("Email", entity.Email, request.Email),
            ("Phone", entity.Phone, request.Phone),
            ("Notes", entity.Notes, request.Notes),
            ("Categories", entity.Categories, request.Categories));

        entity.Name = request.Name.Trim();
        entity.Role = NormalizeOptional(request.Role);
        entity.Organization = NormalizeOptional(request.Organization);
        entity.Address = NormalizeOptional(request.Address);
        entity.Office = NormalizeOptional(request.Office);
        entity.Email = NormalizeOptional(request.Email);
        entity.Phone = NormalizeOptional(request.Phone);
        entity.Notes = NormalizeOptional(request.Notes);
        entity.Categories = request.Categories == ContactCategory.None ? ContactCategory.Custom : request.Categories;
        entity.UpdatedAt = DateTime.UtcNow;

        using var tx = await db.Database.BeginTransactionAsync(ct);
        await audit.LogAsync("Update", nameof(Contact), entity.Id, details, currentUser.UserId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        TikrActionLog.Completed(_log, "Contact.Update",
            $"ContactId={entity.Id} Category={entity.Categories} UserId={currentUser.UserId}");
        return entity;
    }

    public async Task SoftDeleteAsync(
        Guid id,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        TikrActionLog.Started(_log, "Contact.SoftDelete",
            $"ContactId={id} UserId={currentUser.UserId}");

        var entity = await db.Contacts.FindAsync([id], ct)
                     ?? throw new KeyNotFoundException($"Contact {id} not found.");
        if (entity.DeletedAt is not null)
            return;

        using var tx = await db.Database.BeginTransactionAsync(ct);
        entity.DeletedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        await audit.LogAsync("SoftDelete", nameof(Contact), id, entity.Name, currentUser.UserId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        TikrActionLog.Completed(_log, "Contact.SoftDelete",
            $"ContactId={id} Category={entity.Categories} UserId={currentUser.UserId}");
    }

    public async Task<Contact> RestoreAsync(
        Guid id,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        TikrActionLog.Started(_log, "Contact.Restore",
            $"ContactId={id} UserId={currentUser.UserId}");

        var entity = await db.Contacts.FindAsync([id], ct)
                     ?? throw new KeyNotFoundException($"Contact {id} not found.");
        if (entity.DeletedAt is null)
            return entity;

        using var tx = await db.Database.BeginTransactionAsync(ct);
        entity.DeletedAt = null;
        entity.UpdatedAt = DateTime.UtcNow;
        await audit.LogAsync("Restore", nameof(Contact), id, entity.Name, currentUser.UserId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        TikrActionLog.Completed(_log, "Contact.Restore",
            $"ContactId={id} Category={entity.Categories} UserId={currentUser.UserId}");
        return entity;
    }

    public async Task LinkToRequirementAsync(
        Guid requirementId,
        Guid contactId,
        bool isPrimary,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        TikrActionLog.Started(_log, "Contact.LinkRequirement",
            $"ContactId={contactId} RequirementId={requirementId} Primary={isPrimary} UserId={currentUser.UserId}");

        var requirement = await db.Requirements.FindAsync([requirementId], ct)
                          ?? throw new KeyNotFoundException($"Requirement {requirementId} not found.");
        var contact = await db.Contacts.FindAsync([contactId], ct)
                      ?? throw new KeyNotFoundException($"Contact {contactId} not found.");
        if (contact.DeletedAt is not null)
            throw new InvalidOperationException("Cannot link a deleted contact.");

        using var tx = await db.Database.BeginTransactionAsync(ct);

        if (isPrimary)
        {
            var existingPrimary = await db.RequirementContacts
                .Where(rc => rc.RequirementId == requirementId && rc.IsPrimary)
                .ToListAsync(ct);
            foreach (var p in existingPrimary)
                p.IsPrimary = false;
        }

        var link = await db.RequirementContacts.FindAsync([requirementId, contactId], ct);
        if (link is null)
        {
            link = new RequirementContact
            {
                RequirementId = requirementId,
                ContactId = contactId,
                LinkedAt = DateTime.UtcNow,
                IsPrimary = isPrimary
            };
            db.RequirementContacts.Add(link);
        }
        else
        {
            link.IsPrimary = isPrimary || link.IsPrimary;
        }

        if (link.IsPrimary || isPrimary)
        {
            requirement.ContactName = contact.Name;
            requirement.ContactEmail = contact.Email;
            requirement.ContactPhone = contact.Phone;
            requirement.UpdatedAt = DateTime.UtcNow;
        }

        await audit.LogAsync(
            "Link",
            nameof(Requirement),
            requirementId,
            $"ContactId={contactId}; Name={contact.Name}; Primary={isPrimary}",
            currentUser.UserId,
            ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        TikrActionLog.Completed(_log, "Contact.LinkRequirement",
            $"ContactId={contactId} Category={contact.Categories} RequirementId={requirementId} UserId={currentUser.UserId}");
    }

    public async Task UnlinkFromRequirementAsync(
        Guid requirementId,
        Guid contactId,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        TikrActionLog.Started(_log, "Contact.UnlinkRequirement",
            $"ContactId={contactId} RequirementId={requirementId} UserId={currentUser.UserId}");

        var link = await db.RequirementContacts.FindAsync([requirementId, contactId], ct)
                   ?? throw new KeyNotFoundException($"Link not found for requirement {requirementId} and contact {contactId}.");

        using var tx = await db.Database.BeginTransactionAsync(ct);
        var wasPrimary = link.IsPrimary;
        db.RequirementContacts.Remove(link);

        if (wasPrimary)
        {
            var requirement = await db.Requirements.FindAsync([requirementId], ct);
            if (requirement is not null)
            {
                var next = await db.RequirementContacts
                    .Include(rc => rc.Contact)
                    .Where(rc => rc.RequirementId == requirementId && rc.ContactId != contactId && rc.Contact.DeletedAt == null)
                    .OrderBy(rc => rc.LinkedAt)
                    .FirstOrDefaultAsync(ct);
                if (next is not null)
                {
                    next.IsPrimary = true;
                    requirement.ContactName = next.Contact.Name;
                    requirement.ContactEmail = next.Contact.Email;
                    requirement.ContactPhone = next.Contact.Phone;
                }
                else
                {
                    requirement.ContactName = null;
                    requirement.ContactEmail = null;
                    requirement.ContactPhone = null;
                }
                requirement.UpdatedAt = DateTime.UtcNow;
            }
        }

        await audit.LogAsync(
            "Unlink",
            nameof(Requirement),
            requirementId,
            $"ContactId={contactId}",
            currentUser.UserId,
            ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        TikrActionLog.Completed(_log, "Contact.UnlinkRequirement",
            $"ContactId={contactId} RequirementId={requirementId} UserId={currentUser.UserId}");
    }

    public async Task<IReadOnlyList<(Contact Contact, bool IsPrimary)>> ListForRequirementAsync(
        Guid requirementId,
        CancellationToken ct = default)
    {
        var rows = await db.RequirementContacts
            .AsNoTracking()
            .Include(rc => rc.Contact)
            .Where(rc => rc.RequirementId == requirementId && rc.Contact.DeletedAt == null)
            .OrderByDescending(rc => rc.IsPrimary)
            .ThenBy(rc => rc.Contact.Name)
            .ToListAsync(ct);

        return rows.Select(rc => (rc.Contact, rc.IsPrimary)).ToList();
    }

    public async Task<(Contact Contact, bool Created)> UpsertAsync(
        CreateContactRequest request,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        var email = NormalizeOptional(request.Email);
        var name = request.Name.Trim();
        var org = NormalizeOptional(request.Organization);

        Contact? existing = null;
        if (email is not null)
        {
            existing = await db.Contacts
                .Where(c => c.DeletedAt == null && c.Email != null && c.Email.ToLower() == email.ToLower())
                .FirstOrDefaultAsync(ct);
        }

        if (existing is null)
        {
            existing = await db.Contacts
                .Where(c => c.DeletedAt == null &&
                            c.Name.ToLower() == name.ToLower() &&
                            ((org == null && c.Organization == null) ||
                             (org != null && c.Organization != null && c.Organization.ToLower() == org.ToLower())))
                .FirstOrDefaultAsync(ct);
        }

        if (existing is null)
            return (await CreateAsync(request, audit, currentUser, ct), true);

        var mergedCategories = existing.Categories | (request.Categories == ContactCategory.None
            ? ContactCategory.Custom
            : request.Categories);

        var update = new UpdateContactRequest(
            Name: string.IsNullOrWhiteSpace(request.Name) ? existing.Name : request.Name.Trim(),
            Role: NormalizeOptional(request.Role) ?? existing.Role,
            Organization: org ?? existing.Organization,
            Address: NormalizeOptional(request.Address) ?? existing.Address,
            Office: NormalizeOptional(request.Office) ?? existing.Office,
            Email: email ?? existing.Email,
            Phone: NormalizeOptional(request.Phone) ?? existing.Phone,
            Notes: MergeNotes(existing.Notes, NormalizeOptional(request.Notes)),
            Categories: mergedCategories);

        var updated = await UpdateAsync(existing.Id, update, audit, currentUser, ct);
        return (updated, false);
    }

    private static string? MergeNotes(string? existing, string? incoming)
    {
        if (string.IsNullOrWhiteSpace(incoming))
            return existing;
        if (string.IsNullOrWhiteSpace(existing))
            return incoming;
        if (existing.Contains(incoming, StringComparison.OrdinalIgnoreCase))
            return existing;
        var merged = existing.TrimEnd() + "\n" + incoming.Trim();
        return merged.Length <= 2000 ? merged : merged[..2000];
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
