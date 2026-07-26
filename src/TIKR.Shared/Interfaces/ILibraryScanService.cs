using TIKR.Shared.DTOs;

namespace TIKR.Shared.Interfaces;

/// <summary>
/// Recursively scans an existing NAS document folder, copies files into TIKR storage,
/// tags/embeds them for Assistant RAG. Source files are left untouched.
/// </summary>
public interface ILibraryScanService
{
    /// <summary>True when <c>TIKR_LIBRARY_SCAN_PATH</c> is configured.</summary>
    bool IsConfigured { get; }

    /// <summary>Configured root path (may be null when not configured).</summary>
    string? LibraryPath { get; }

    /// <summary>Poll interval for the background hosted service.</summary>
    int IntervalSeconds { get; }

    /// <summary>Last completed scan result, if any.</summary>
    LibraryScanResult? LastResult { get; }

    /// <summary>UTC timestamp of last completed scan, if any.</summary>
    DateTime? LastScanUtc { get; }

    /// <summary>Run one scan pass (up to the per-run import cap).</summary>
    Task<LibraryScanResult> ScanAsync(CancellationToken ct = default);

    /// <summary>Snapshot for Settings / status API.</summary>
    LibraryScanStatusDto GetStatus();
}
