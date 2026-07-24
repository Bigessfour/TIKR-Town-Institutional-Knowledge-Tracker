using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of document business logic (upload, validation, persistence + audit + tx).
/// Extracted from Program.cs per final backend cleanup recommendations for separation and testability.
/// </summary>
public class DocumentService(TikrDbContext db) : IDocumentService
{
    public async Task<Document> UploadAsync(
        Stream content,
        string fileName,
        string? contentType,
        long length,
        IFileStorageService storage,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        // Centralized validation (best practice)
        if (content == null) throw new ArgumentException("No file uploaded.");
        if (length > 100 * 1024 * 1024) throw new ArgumentException("File too large (max 100MB).");
        if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Invalid filename.");

        var (entity, _, _) = await PrepareDocumentUploadAsync(content, fileName, contentType, length, storage, ct);

        using var tx = await db.Database.BeginTransactionAsync(ct);
        db.Documents.Add(entity);
        await audit.LogAsync("Upload", nameof(Document), entity.Id, entity.FileName, currentUser.UserId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return entity;
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

    public async Task DeleteAsync(
        Guid id,
        IFileStorageService storage,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        var entity = await db.Documents.FindAsync(id, ct);
        if (entity is null) throw new KeyNotFoundException($"Document {id} not found.");

        using var tx = await db.Database.BeginTransactionAsync(ct);
        var chunks = await db.EmbeddingChunks
            .Where(c => c.SourceType == EmbeddingSourceType.Document && c.SourceId == id)
            .ToListAsync(ct);
        if (chunks.Count > 0)
            db.EmbeddingChunks.RemoveRange(chunks);
        db.Documents.Remove(entity);
        await audit.LogAsync("Delete", nameof(Document), id, entity.FileName, currentUser.UserId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // Delete file from storage only after successful commit to avoid record-without-file or file-without-record.
        // Best-effort: ignore storage errors (e.g. already deleted) so DB+audit removal is not rolled back.
        if (!string.IsNullOrWhiteSpace(entity.StoragePath))
        {
            try
            {
                await storage.DeleteAsync(entity.StoragePath, ct);
            }
            catch
            {
                // best effort cleanup; record removal takes precedence for consistency
            }
        }
    }
}
