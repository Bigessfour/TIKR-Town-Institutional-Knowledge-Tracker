namespace TIKR.Shared.DTOs;

/// <summary>Result of one NAS library scan pass (copy → tag/embed).</summary>
public record LibraryScanResult(
    int Scanned,
    int Imported,
    int Skipped,
    int Failed,
    IReadOnlyList<string> Errors);

/// <summary>Operator status for library scan configuration and last run.</summary>
public record LibraryScanStatusDto(
    bool Configured,
    string? LibraryPath,
    int IntervalSeconds,
    bool PollerActive,
    LibraryScanResult? LastResult,
    DateTime? LastScanUtc,
    /// <summary>True while a scan is in-flight (manual or hosted poller). Concurrent scans are single-flight.</summary>
    bool ScanInProgress = false);
