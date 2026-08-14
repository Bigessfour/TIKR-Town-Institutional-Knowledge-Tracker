using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;

namespace TIKR.Shared.Interfaces;

public interface IContactService
{
    Task<IReadOnlyList<Contact>> ListAsync(
        string? query = null,
        ContactCategory? category = null,
        bool deleted = false,
        CancellationToken ct = default);

    Task<Contact?> GetAsync(Guid id, CancellationToken ct = default);

    Task<Contact> CreateAsync(
        CreateContactRequest request,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default);

    Task<Contact> UpdateAsync(
        Guid id,
        UpdateContactRequest request,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default);

    Task SoftDeleteAsync(
        Guid id,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default);

    Task<Contact> RestoreAsync(
        Guid id,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default);

    Task LinkToRequirementAsync(
        Guid requirementId,
        Guid contactId,
        bool isPrimary,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default);

    Task UnlinkFromRequirementAsync(
        Guid requirementId,
        Guid contactId,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default);

    Task<IReadOnlyList<(Contact Contact, bool IsPrimary)>> ListForRequirementAsync(
        Guid requirementId,
        CancellationToken ct = default);
}
