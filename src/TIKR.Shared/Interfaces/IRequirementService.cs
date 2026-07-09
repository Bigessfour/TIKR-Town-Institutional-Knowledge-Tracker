using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;

namespace TIKR.Shared.Interfaces;

/// <summary>
/// Centralized business logic for Requirements (CRUD, links, seeded guards, audit, tx).
/// Extracted as part of final backend cleanup for clean layering and testability.
/// </summary>
public interface IRequirementService
{
    Task<Requirement> CreateAsync(CreateRequirementRequest request, IAuditService audit, ICurrentUserService currentUser, CancellationToken ct = default);
    Task<Requirement> UpdateAsync(Guid id, UpdateRequirementRequest request, IAuditService audit, ICurrentUserService currentUser, CancellationToken ct = default);
    Task DeleteAsync(Guid id, IAuditService audit, ICurrentUserService currentUser, CancellationToken ct = default);

    /// <summary>
    /// Link a document to requirement (idempotent) inside transaction + audit log.
    /// </summary>
    Task LinkDocumentAsync(Guid requirementId, Guid documentId, IAuditService audit, ICurrentUserService currentUser, CancellationToken ct = default);

    /// <summary>
    /// Unlink document from requirement inside transaction + audit log.
    /// </summary>
    Task UnlinkDocumentAsync(Guid requirementId, Guid documentId, IAuditService audit, ICurrentUserService currentUser, CancellationToken ct = default);
}
