using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Diagnostics;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;
using TIKR.Shared.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of document business logic (upload, validation, persistence + audit + tx).
/// Extracted from Program.cs per final backend cleanup recommendations for separation and testability.
/// </summary>
public class DocumentService(TikrDbContext db, ILogger<DocumentService>? logger = null) : IDocumentService
{
    private readonly ILogger _log = logger ?? NullLogger<DocumentService>.Instance;

    public async Task<Document> UploadAsync(
        Stream content,
        string fileName,
        string? contentType,
        long length,
        IFileStorageService storage,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default,
        bool isTransient = false)
    {
        var sw = Stopwatch.StartNew();
        TikrActionLog.Started(_log, "Document.Upload",
            $"FileName={fileName} Bytes={length} Transient={isTransient}");

        // Centralized validation (best practice)
        if (content == null) throw new ArgumentException("No file uploaded.");
        if (length > 100 * 1024 * 1024) throw new ArgumentException("File too large (max 100MB).");
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Invalid filename.");

        try
        {
            var (entity, fullText, storagePath) = await PrepareDocumentUploadAsync(content, fileName, contentType, length, storage, ct);
            entity.IsTransient = isTransient;

            using var tx = await db.Database.BeginTransactionAsync(ct);
            db.Documents.Add(entity);
            await audit.LogAsync("Upload", nameof(Document), entity.Id, entity.FileName, currentUser.UserId, ct);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            TikrActionLog.Completed(_log, "Document.Upload",
                $"DocumentId={entity.Id} FileName={entity.FileName} StoragePath={storagePath} TextChars={fullText?.Length ?? 0}",
                sw.ElapsedMilliseconds);
            return entity;
        }
        catch (Exception ex)
        {
            TikrActionLog.Failed(_log, "Document.Upload", ex, $"FileName={fileName}");
            throw;
        }
    }

    public async Task<(Document Entity, string? FullText, string StoragePath)> PrepareDocumentUploadAsync(
        Stream content,
        string fileName,
        string? contentType,
        long length,
        IFileStorageService storage,
        CancellationToken ct = default)
    {
        string storagePath;
        string? fullText = null;

        // Reset for read
        if (content.CanSeek) content.Position = 0;

        if (DocumentTextExtractionService.CanExtract(fileName))
        {
            await using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, ct);
            buffer.Position = 0;
            fullText = await DocumentTextExtractionService.TryExtractAsync(buffer, fileName, ct);
            buffer.Position = 0;
            storagePath = await storage.SaveAsync(buffer, fileName, ct);
        }
        else
        {
            if (content.CanSeek) content.Position = 0;
            storagePath = await storage.SaveAsync(content, fileName, ct);
        }

        var entity = new Document
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            StoragePath = storagePath,
            ContentType = contentType,
            FileSizeBytes = length,
            FullTextContent = fullText,
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        return (entity, fullText, storagePath);
    }

    /// <summary>Max prior content versions retained per document (oldest purged).</summary>
    public const int MaxVersionsPerDocument = 10;

    /// <summary>
    /// Soft-delete for clerk recovery (recycle bin). Prefer SoftDeleteAsync from API DELETE.
    /// Hard purge remains available via PurgeAsync.
    /// </summary>
    public Task DeleteAsync(
        Guid id,
        IFileStorageService storage,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default) =>
        SoftDeleteAsync(id, audit, currentUser, ct);

    public async Task SoftDeleteAsync(
        Guid id,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        TikrActionLog.Started(_log, "Document.SoftDelete", $"DocumentId={id}");

        try
        {
            var entity = await db.Documents.FindAsync([id], ct)
                         ?? throw new KeyNotFoundException($"Document {id} not found.");
            if (entity.DeletedAt is not null)
                return;

            using var tx = await db.Database.BeginTransactionAsync(ct);
            // Drop RAG chunks so Assistant no longer cites deleted docs.
            var chunks = await db.EmbeddingChunks
                .Where(c => c.SourceType == EmbeddingSourceType.Document && c.SourceId == id)
                .ToListAsync(ct);
            if (chunks.Count > 0)
                db.EmbeddingChunks.RemoveRange(chunks);

            entity.DeletedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.Embedding = null;
            await audit.LogAsync("SoftDelete", nameof(Document), id, entity.FileName, currentUser.UserId, ct);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            TikrActionLog.Completed(_log, "Document.SoftDelete",
                $"DocumentId={id} FileName={entity.FileName} ChunksRemoved={chunks.Count}",
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            TikrActionLog.Failed(_log, "Document.SoftDelete", ex, $"DocumentId={id}");
            throw;
        }
    }

    public async Task RestoreAsync(
        Guid id,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        TikrActionLog.Started(_log, "Document.Restore", $"DocumentId={id}");
        try
        {
            var entity = await db.Documents.FindAsync([id], ct)
                         ?? throw new KeyNotFoundException($"Document {id} not found.");
            if (entity.DeletedAt is null)
                return;

            entity.DeletedAt = null;
            entity.UpdatedAt = DateTime.UtcNow;
            await audit.LogAsync("Restore", nameof(Document), id, entity.FileName, currentUser.UserId, ct);
            await db.SaveChangesAsync(ct);
            TikrActionLog.Completed(_log, "Document.Restore",
                $"DocumentId={id} FileName={entity.FileName}", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            TikrActionLog.Failed(_log, "Document.Restore", ex, $"DocumentId={id}");
            throw;
        }
    }

    public async Task PurgeAsync(
        Guid id,
        IFileStorageService storage,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        TikrActionLog.Started(_log, "Document.Purge", $"DocumentId={id}");

        try
        {
            var entity = await db.Documents.FindAsync([id], ct)
                         ?? throw new KeyNotFoundException($"Document {id} not found.");

            var versions = await db.DocumentVersions.Where(v => v.DocumentId == id).ToListAsync(ct);
            var paths = new List<string>();
            if (!string.IsNullOrWhiteSpace(entity.StoragePath))
                paths.Add(entity.StoragePath);
            paths.AddRange(versions.Select(v => v.StoragePath).Where(p => !string.IsNullOrWhiteSpace(p)));

            using var tx = await db.Database.BeginTransactionAsync(ct);
            var chunks = await db.EmbeddingChunks
                .Where(c => c.SourceType == EmbeddingSourceType.Document && c.SourceId == id)
                .ToListAsync(ct);
            if (chunks.Count > 0)
                db.EmbeddingChunks.RemoveRange(chunks);
            if (versions.Count > 0)
                db.DocumentVersions.RemoveRange(versions);
            db.Documents.Remove(entity);
            await audit.LogAsync("Purge", nameof(Document), id, entity.FileName, currentUser.UserId, ct);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            foreach (var path in paths.Distinct(StringComparer.Ordinal))
            {
                try { await storage.DeleteAsync(path, ct); }
                catch (Exception ex)
                {
                    TikrActionLog.Info(_log, "Document.Purge",
                        $"Storage cleanup best-effort failed for {path}: {ex.Message}");
                }
            }

            TikrActionLog.Completed(_log, "Document.Purge",
                $"DocumentId={id} FileName={entity.FileName} VersionsPurged={versions.Count}",
                sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            TikrActionLog.Failed(_log, "Document.Purge", ex, $"DocumentId={id}");
            throw;
        }
    }

    public async Task<Document> ReplaceContentAsync(
        Guid id,
        Stream content,
        string? contentType,
        long length,
        IFileStorageService storage,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        TikrActionLog.Started(_log, "Document.ReplaceContent", $"DocumentId={id} Bytes={length}");

        try
        {
            if (content is null) throw new ArgumentException("No file content.");
            if (length > 100 * 1024 * 1024) throw new ArgumentException("File too large (max 100MB).");

            var entity = await db.Documents.FindAsync([id], ct)
                         ?? throw new KeyNotFoundException($"Document {id} not found.");
            if (entity.DeletedAt is not null)
                throw new InvalidOperationException("Cannot replace content of a deleted document. Restore it first.");

            if (content.CanSeek) content.Position = 0;

            string? fullText = null;
            string newPath;
            if (DocumentTextExtractionService.CanExtract(entity.FileName))
            {
                await using var buffer = new MemoryStream();
                await content.CopyToAsync(buffer, ct);
                buffer.Position = 0;
                fullText = await DocumentTextExtractionService.TryExtractAsync(buffer, entity.FileName, ct);
                buffer.Position = 0;
                newPath = await storage.SaveAsync(buffer, entity.FileName, ct);
            }
            else
            {
                if (content.CanSeek) content.Position = 0;
                newPath = await storage.SaveAsync(content, entity.FileName, ct);
            }

            var oldPath = entity.StoragePath;
            var oldSize = entity.FileSizeBytes;
            var oldType = entity.ContentType;
            var details = AuditChangeBuilder.Build(
                entity.FileName,
                ("FileSizeBytes", entity.FileSizeBytes, length),
                ("ContentType", entity.ContentType, contentType ?? entity.ContentType),
                ("StoragePath", entity.StoragePath, newPath));

            using var tx = await db.Database.BeginTransactionAsync(ct);
            var chunks = await db.EmbeddingChunks
                .Where(c => c.SourceType == EmbeddingSourceType.Document && c.SourceId == id)
                .ToListAsync(ct);
            if (chunks.Count > 0)
                db.EmbeddingChunks.RemoveRange(chunks);

            // Retain prior bytes as a version (do not delete old storage immediately).
            if (!string.IsNullOrWhiteSpace(oldPath) &&
                !string.Equals(oldPath, newPath, StringComparison.Ordinal))
            {
                await AddVersionSnapshotAsync(entity, oldPath, oldSize, oldType, "Before save", currentUser.UserId, ct);
            }

            entity.StoragePath = newPath;
            entity.ContentType = contentType ?? entity.ContentType;
            entity.FileSizeBytes = length;
            entity.FullTextContent = fullText;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.Embedding = null;

            await audit.LogAsync("Update", nameof(Document), id, details, currentUser.UserId, ct);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await PruneOldVersionsAsync(id, storage, ct);

            TikrActionLog.Completed(_log, "Document.ReplaceContent",
                $"DocumentId={id} FileName={entity.FileName} TextChars={fullText?.Length ?? 0}",
                sw.ElapsedMilliseconds);
            return entity;
        }
        catch (Exception ex)
        {
            TikrActionLog.Failed(_log, "Document.ReplaceContent", ex, $"DocumentId={id}");
            throw;
        }
    }

    public async Task<IReadOnlyList<DocumentVersion>> ListVersionsAsync(Guid documentId, CancellationToken ct = default)
    {
        return await db.DocumentVersions
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(ct);
    }

    public async Task<Document> RestoreVersionAsync(
        Guid documentId,
        Guid versionId,
        IFileStorageService storage,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        var version = await db.DocumentVersions
                          .FirstOrDefaultAsync(v => v.Id == versionId && v.DocumentId == documentId, ct)
                      ?? throw new KeyNotFoundException($"Version {versionId} not found.");

        await using var stream = await storage.OpenReadAsync(version.StoragePath, ct);
        await using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct);
        buffer.Position = 0;
        return await ReplaceContentAsync(
            documentId,
            buffer,
            version.ContentType,
            buffer.Length,
            storage,
            audit,
            currentUser,
            ct);
    }

    private async Task AddVersionSnapshotAsync(
        Document entity,
        string oldPath,
        long oldSize,
        string? oldType,
        string note,
        string? userId,
        CancellationToken ct)
    {
        var nextNum = await db.DocumentVersions
            .Where(v => v.DocumentId == entity.Id)
            .Select(v => (int?)v.VersionNumber)
            .MaxAsync(ct) ?? 0;
        nextNum++;

        db.DocumentVersions.Add(new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = entity.Id,
            VersionNumber = nextNum,
            FileName = entity.FileName,
            StoragePath = oldPath,
            ContentType = oldType,
            FileSizeBytes = oldSize,
            Note = note,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        });
    }

    private async Task PruneOldVersionsAsync(Guid documentId, IFileStorageService storage, CancellationToken ct)
    {
        var excess = await db.DocumentVersions
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .Skip(MaxVersionsPerDocument)
            .ToListAsync(ct);
        if (excess.Count == 0)
            return;

        foreach (var v in excess)
        {
            try { await storage.DeleteAsync(v.StoragePath, ct); }
            catch { /* best effort */ }
        }

        db.DocumentVersions.RemoveRange(excess);
        await db.SaveChangesAsync(ct);
    }

    public async Task<Document> UpdateMetadataAsync(
        Guid id,
        string? fileName,
        string? suggestedFolder,
        bool updateFolder,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        TikrActionLog.Started(_log, "Document.UpdateMetadata", $"DocumentId={id}");

        try
        {
            var entity = await db.Documents.FindAsync([id], ct)
                         ?? throw new KeyNotFoundException($"Document {id} not found.");

            var changes = new List<(string Field, object? From, object? To)>();
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                var safe = Path.GetFileName(fileName.Trim());
                if (string.IsNullOrWhiteSpace(safe))
                    throw new ArgumentException("Invalid filename.");
                if (!string.Equals(entity.FileName, safe, StringComparison.Ordinal))
                {
                    changes.Add(("FileName", entity.FileName, safe));
                    entity.FileName = safe;
                }
            }

            if (updateFolder)
            {
                var folder = string.IsNullOrWhiteSpace(suggestedFolder) ? null : suggestedFolder.Trim();
                if (!string.Equals(entity.SuggestedFolder, folder, StringComparison.Ordinal))
                {
                    changes.Add(("SuggestedFolder", entity.SuggestedFolder, folder));
                    entity.SuggestedFolder = folder;
                }
            }

            if (changes.Count == 0)
                return entity;

            entity.UpdatedAt = DateTime.UtcNow;
            var details = AuditChangeBuilder.Build(
                entity.FileName,
                changes.Select(c => (c.Field, c.From, c.To)).ToArray());

            using var tx = await db.Database.BeginTransactionAsync(ct);
            await audit.LogAsync("Update", nameof(Document), id, details, currentUser.UserId, ct);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            TikrActionLog.Completed(_log, "Document.UpdateMetadata",
                $"DocumentId={id} FileName={entity.FileName} Folder={entity.SuggestedFolder ?? "(none)"}",
                sw.ElapsedMilliseconds);
            return entity;
        }
        catch (Exception ex)
        {
            TikrActionLog.Failed(_log, "Document.UpdateMetadata", ex, $"DocumentId={id}");
            throw;
        }
    }
}
