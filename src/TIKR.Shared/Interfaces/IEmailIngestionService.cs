using TIKR.Shared.DTOs;

namespace TIKR.Shared.Interfaces;

/// <summary>
/// Forward-to-folder email ingestion scaffold (local inbox directory → Documents).
/// Real IMAP can replace the watcher later without changing callers.
/// </summary>
public interface IEmailIngestionService
{
    /// <summary>True when <c>TIKR_EMAIL_INBOX_PATH</c> is configured.</summary>
    bool IsConfigured { get; }

    /// <summary>Ingest pending files from the inbox folder into document storage.</summary>
    Task<EmailIngestionResult> IngestPendingAsync(CancellationToken ct = default);
}
