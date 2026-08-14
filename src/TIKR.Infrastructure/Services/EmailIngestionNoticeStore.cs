using TIKR.Shared.DTOs;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

public sealed class EmailIngestionNoticeStore : IEmailIngestionNoticeStore
{
    private readonly object _gate = new();
    private readonly LinkedList<EmailExtractionNoticeDto> _notices = new();
    private const int MaxNotices = 40;

    public void Add(EmailExtractionNoticeDto notice)
    {
        lock (_gate)
        {
            _notices.AddFirst(notice);
            while (_notices.Count > MaxNotices)
                _notices.RemoveLast();
        }
    }

    public IReadOnlyList<EmailExtractionNoticeDto> GetRecent(int take = 10)
    {
        lock (_gate)
            return _notices.Take(Math.Clamp(take, 1, MaxNotices)).ToList();
    }

    public void Clear()
    {
        lock (_gate)
            _notices.Clear();
    }
}
