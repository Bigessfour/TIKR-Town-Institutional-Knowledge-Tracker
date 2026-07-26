using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Configuration;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// Recursively scans <c>TIKR_LIBRARY_SCAN_PATH</c>, copies allowed files into TIKR Documents,
/// then tags/embeds for Assistant RAG. Source NAS files are never moved or deleted.
/// Concurrent scan requests are single-flight (manual Settings scan + hosted poller).
/// </summary>
public sealed class LibraryScanService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<LibraryScanService> logger) : ILibraryScanService
{
    public const int DefaultMaxImportsPerRun = 500;

    public int MaxImportsPerRun => TikrConfiguration.GetLibraryScanMaxImportsPerRun(configuration);

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Town office formats (no TIFF/image archives — clerks use PDF/Word)
        ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".txt", ".md", ".csv", ".eml", ".msg"
    };

    private readonly object _lastLock = new();
    private readonly SemaphoreSlim _scanGate = new(1, 1);
    private LibraryScanResult? _lastResult;
    private DateTime? _lastScanUtc;
    private int _scanInProgress;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(LibraryPath);

    public string? LibraryPath => TikrConfiguration.GetLibraryScanPath(configuration);

    public int IntervalSeconds => TikrConfiguration.GetLibraryScanIntervalSeconds(configuration);

    public LibraryScanResult? LastResult
    {
        get { lock (_lastLock) return _lastResult; }
    }

    public DateTime? LastScanUtc
    {
        get { lock (_lastLock) return _lastScanUtc; }
    }

    public bool ScanInProgress => Volatile.Read(ref _scanInProgress) != 0;

    public LibraryScanStatusDto GetStatus() =>
        new(
            IsConfigured,
            LibraryPath,
            IntervalSeconds,
            PollerActive: IsConfigured,
            LastResult,
            LastScanUtc,
            ScanInProgress);

    public async Task<LibraryScanResult> ScanAsync(CancellationToken ct = default)
    {
        // Single-flight: hosted poller + manual POST must not import the same path twice.
        // The waiter re-runs after the active scan finishes; fingerprint skips make that cheap.
        if (!await _scanGate.WaitAsync(TimeSpan.Zero, ct))
        {
            logger.LogInformation("Library scan already in progress; serializing behind the active scan");
            await _scanGate.WaitAsync(ct);
        }

        Interlocked.Exchange(ref _scanInProgress, 1);
        try
        {
            return await ScanCoreAsync(ct);
        }
        finally
        {
            Interlocked.Exchange(ref _scanInProgress, 0);
            _scanGate.Release();
        }
    }

    private async Task<LibraryScanResult> ScanCoreAsync(CancellationToken ct)
    {
        var root = LibraryPath;
        if (string.IsNullOrWhiteSpace(root))
        {
            var missing = new LibraryScanResult(0, 0, 0, 0,
                ["Library scan path is not configured (TIKR_LIBRARY_SCAN_PATH)."]);
            Remember(missing);
            return missing;
        }

        if (!Directory.Exists(root))
        {
            var missingDir = new LibraryScanResult(0, 0, 0, 0,
                [$"Library scan path does not exist: {root}"]);
            Remember(missingDir);
            return missingDir;
        }

        var errors = new List<string>();
        var scanned = 0;
        var imported = 0;
        var skipped = 0;
        var failed = 0;

        foreach (var filePath in EnumerateLibraryFiles(root))
        {
            ct.ThrowIfCancellationRequested();
            scanned++;

            if (imported >= MaxImportsPerRun)
            {
                skipped++;
                continue;
            }

            var relativePath = Path.GetRelativePath(root, filePath);
            // Normalize separators so RelativePath uniqueness is stable across platforms.
            relativePath = relativePath.Replace('\\', '/');
            var fingerprint = BuildFingerprint(filePath);

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<TikrDbContext>();
                var existing = await db.LibraryImportRecords
                    .FirstOrDefaultAsync(r => r.RelativePath == relativePath, ct);

                if (existing is not null && existing.ContentFingerprint == fingerprint)
                {
                    skipped++;
                    continue;
                }

                var documents = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
                var audit = scope.ServiceProvider.GetRequiredService<IAuditService>();
                var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();
                var ai = scope.ServiceProvider.GetRequiredService<IHybridAiService>();

                var fileName = Path.GetFileName(filePath);
                var ext = Path.GetExtension(fileName);
                await using var stream = File.OpenRead(filePath);
                var contentType = GuessContentType(ext);
                var document = await documents.UploadAsync(
                    stream,
                    fileName,
                    contentType,
                    stream.Length,
                    storage,
                    audit,
                    currentUser,
                    ct);

                try
                {
                    await ai.TagDocumentAsync(document.Id, ct);
                }
                catch (Exception tagEx)
                {
                    logger.LogWarning(tagEx,
                        "Library scan uploaded {RelativePath} but tag/embed failed; document {DocumentId} remains searchable after reindex",
                        relativePath, document.Id);
                    errors.Add($"{relativePath}: tag/embed — {tagEx.Message}");
                }

                // Re-check after long tag/embed work in case another process wrote the claim.
                existing ??= await db.LibraryImportRecords
                    .FirstOrDefaultAsync(r => r.RelativePath == relativePath, ct);

                if (existing is null)
                {
                    db.LibraryImportRecords.Add(new LibraryImportRecord
                    {
                        Id = Guid.NewGuid(),
                        RelativePath = relativePath,
                        ContentFingerprint = fingerprint,
                        DocumentId = document.Id,
                        ImportedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    existing.ContentFingerprint = fingerprint;
                    existing.DocumentId = document.Id;
                    existing.ImportedAt = DateTime.UtcNow;
                }

                try
                {
                    await db.SaveChangesAsync(ct);
                }
                catch (DbUpdateException ex) when (IsUniqueRelativePathViolation(ex))
                {
                    // Defensive: treat as skip rather than failed after a successful upload.
                    logger.LogInformation(
                        ex,
                        "Library scan import record race for {RelativePath}; treating as skipped",
                        relativePath);
                    skipped++;
                    continue;
                }

                imported++;
                logger.LogInformation(
                    "Library scan imported {RelativePath} as document {DocumentId}",
                    relativePath, document.Id);
            }
            catch (Exception ex)
            {
                failed++;
                logger.LogWarning(ex, "Library scan failed for {RelativePath}", relativePath);
                errors.Add($"{relativePath}: {ex.Message}");
            }
        }

        var result = new LibraryScanResult(scanned, imported, skipped, failed, errors);
        Remember(result);
        return result;
    }

    private void Remember(LibraryScanResult result)
    {
        lock (_lastLock)
        {
            _lastResult = result;
            _lastScanUtc = DateTime.UtcNow;
        }
    }

    internal static bool IsUniqueRelativePathViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("LibraryImportRecords", StringComparison.OrdinalIgnoreCase)
               && (message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                   || message.Contains("unique", StringComparison.OrdinalIgnoreCase));
    }

    internal static IEnumerable<string> EnumerateLibraryFiles(string root)
    {
        foreach (var filePath in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(filePath);
            if (!AllowedExtensions.Contains(ext))
                continue;

            // Skip junk / macOS metadata
            var name = Path.GetFileName(filePath);
            if (name.StartsWith("._", StringComparison.Ordinal) ||
                name.Equals(".DS_Store", StringComparison.OrdinalIgnoreCase))
                continue;

            yield return filePath;
        }
    }

    internal static bool IsAllowedExtension(string path) =>
        AllowedExtensions.Contains(Path.GetExtension(path));

    internal static string BuildFingerprint(string filePath)
    {
        var info = new FileInfo(filePath);
        return $"{info.Length}:{info.LastWriteTimeUtc.Ticks}";
    }

    private static string GuessContentType(string ext) => ext.ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".doc" => "application/msword",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ".xls" => "application/vnd.ms-excel",
        ".txt" or ".md" or ".csv" => "text/plain",
        ".eml" => "message/rfc822",
        _ => "application/octet-stream"
    };
}
