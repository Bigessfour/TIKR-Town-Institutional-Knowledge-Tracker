using TIKR.Shared.DTOs;

namespace TIKR.Shared.Interfaces;

public interface IDocumentAgentService
{
    Task<DocumentAgentResult> ProcessUploadAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default);
}
