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
    // Link/unlink can be added; queries thin for now.
}
