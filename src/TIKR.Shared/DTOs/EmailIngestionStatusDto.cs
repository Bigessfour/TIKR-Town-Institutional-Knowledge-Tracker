namespace TIKR.Shared.DTOs;

/// <summary>Operator status for forward-to-folder email ingestion.</summary>
public record EmailIngestionStatusDto(
    bool Configured,
    string? InboxPath,
    EmailIngestionResult? LastResult,
    DateTime? LastIngestUtc);
