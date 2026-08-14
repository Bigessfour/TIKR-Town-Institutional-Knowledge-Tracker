using TIKR.Shared.DTOs;

namespace TIKR.Shared.Interfaces;

/// <summary>In-memory clerk-facing notices from folder email structured extract.</summary>
public interface IEmailIngestionNoticeStore
{
    void Add(EmailExtractionNoticeDto notice);

    IReadOnlyList<EmailExtractionNoticeDto> GetRecent(int take = 10);

    void Clear();
}
