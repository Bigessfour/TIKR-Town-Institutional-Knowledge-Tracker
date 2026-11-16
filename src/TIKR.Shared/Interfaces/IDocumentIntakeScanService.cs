using TIKR.Shared.DTOs;

namespace TIKR.Shared.Interfaces;

public interface IDocumentIntakeScanService
{
    Task<DocumentIntakeBriefDto> ScanAsync(Guid documentId, CancellationToken ct = default);

    Task<DocumentIntakeBriefDto?> GetPendingAsync(Guid documentId, CancellationToken ct = default);

    Task<AcceptDocumentIntakeBriefResponse> AcceptAsync(
        Guid documentId,
        AcceptDocumentIntakeBriefRequest request,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default);

    Task DismissAsync(Guid documentId, CancellationToken ct = default);
}
