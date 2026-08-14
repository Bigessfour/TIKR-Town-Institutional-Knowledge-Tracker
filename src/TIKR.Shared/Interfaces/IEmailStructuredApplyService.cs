using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Helpers;

namespace TIKR.Shared.Interfaces;

public interface IEmailStructuredApplyService
{
    /// <summary>
    /// Apply extractor output: upsert contacts, optional knowledge entry, notice + requirement suggestion.
    /// Never throws — failures are logged and returned in the notice.
    /// </summary>
    Task<EmailExtractionNoticeDto> ApplyAsync(
        EmailStructuredExtractResult extract,
        string fileName,
        Guid? documentId,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default);
}
