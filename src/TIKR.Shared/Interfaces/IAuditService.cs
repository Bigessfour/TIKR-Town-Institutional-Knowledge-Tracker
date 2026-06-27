namespace TIKR.Shared.Interfaces;

public interface IAuditService
{
    Task LogAsync(string action, string entityType, Guid? entityId = null, string? details = null, string? userId = null, CancellationToken cancellationToken = default);
}
