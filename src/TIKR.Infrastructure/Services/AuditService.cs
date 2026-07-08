using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Entities;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

public class AuditService(TikrDbContext db) : IAuditService
{
    public async Task LogAsync(string action, string entityType, Guid? entityId = null, string? details = null, string? userId = null, CancellationToken cancellationToken = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            UserId = userId,
            Timestamp = DateTime.UtcNow
        });

        // Save immediately unless an explicit transaction is active on the context.
        // This keeps simple call sites (and unit tests) working while allowing outer tx
        // in multi-step operations (e.g. council packet, document upload) for atomicity.
        if (db.Database.CurrentTransaction == null)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
