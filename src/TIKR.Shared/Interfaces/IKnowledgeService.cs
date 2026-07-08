using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;

namespace TIKR.Shared.Interfaces;

/// <summary>
/// Centralized logic for KnowledgeEntry (CRUD + embed side effects + audit).
/// Part of service extraction for backend cleanup.
/// </summary>
public interface IKnowledgeService
{
    Task<KnowledgeEntry> CreateAsync(CreateKnowledgeEntryRequest request, IAuditService audit, IHybridAiService ai, ICurrentUserService currentUser, CancellationToken ct = default);
    Task<KnowledgeEntry> UpdateAsync(Guid id, UpdateKnowledgeEntryRequest request, IAuditService audit, IHybridAiService ai, ICurrentUserService currentUser, CancellationToken ct = default);
    Task DeleteAsync(Guid id, IAuditService audit, ICurrentUserService currentUser, CancellationToken ct = default);
}
