using Microsoft.AspNetCore.Http;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Entities;
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

        // Adapt to internal prep (for now wrap stream; in full would refactor prepare to take Stream+meta)
        // Simple: create temp form-like or delegate. For min, use a wrapper stream + filename.
        // To keep simple, reuse logic by creating MemoryStream copy if needed.
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        // For extraction/storage use the buffer; length from param.
        // Simplified for now - call internal with adapted (production would have overload).
        var (entity, _, _) = await PrepareUploadInternalFromStreamAsync(buffer, fileName, contentType, length, storage, ct);

        using var tx = await db.Database.BeginTransactionAsync(ct);
        db.Documents.Add(entity);
        await audit.LogAsync("Upload", nameof(Document), entity.Id, entity.FileName, currentUser.UserId, ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return entity;
    }

    private static async Task<(Document Entity, string? FullText, string StoragePath)> PrepareUploadInternalFromStreamAsync(
        Stream contentStream,
        string fileName,
        string? contentType,
        long length,
        IFileStorageService storage,
        CancellationToken ct = default)
    {
        string storagePath;
        string? fullText = null;

        // Reset for read
        if (contentStream.CanSeek) contentStream.Position = 0;

        if (DocumentTextExtractionService.CanExtract(fileName))
        {
            await using var buffer = new MemoryStream();
            await contentStream.CopyToAsync(buffer, ct);
            buffer.Position = 0;
            fullText = await DocumentTextExtractionService.TryExtractAsync(buffer, fileName, ct);
            buffer.Position = 0;
            storagePath = await storage.SaveAsync(buffer, fileName, ct);
        }
        else
        {
            if (contentStream.CanSeek) contentStream.Position = 0;
            storagePath = await storage.SaveAsync(contentStream, fileName, ct);
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
}
