using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;

namespace TIKR.Shared.Interfaces;

/// <summary>
/// Centralized business logic for Documents (upload prep, CRUD, validation, audit participation).
/// Extracted from Program.cs for maintainability and testability (final backend cleanup).
/// </summary>
public interface IDocumentService
{
    /// <summary>
    /// Prepares upload (text extraction decision + storage save) returning ready-to-persist entity + metadata.
    /// Does NOT perform DB persist or audit (caller responsible, e.g. UploadAsync).
    /// Accepts portable stream data (avoids web types in Shared interface).
    /// </summary>
    Task<(Document Entity, string? FullText, string StoragePath)> PrepareDocumentUploadAsync(Stream content, string fileName, string? contentType, long length, IFileStorageService storage, CancellationToken ct = default);

    /// <summary>
    /// Prepares upload (text extraction decision + storage) and creates/persists Document entity.
    /// Handles validation, audit, and transaction for atomicity with audit.
    /// Accepts portable stream data (avoids web types in Shared interface).
    /// </summary>
    Task<Document> UploadAsync(
        Stream content,
        string fileName,
        string? contentType,
        long length,
        IFileStorageService storage,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default,
        bool isTransient = false);

    // Future: other CRUD if needed beyond thin endpoints; queries can stay direct for now.

    /// <summary>
    /// Delete document + audit under transaction (storage cleanup best-effort after commit for integrity).
    /// </summary>
    Task DeleteAsync(Guid id, IFileStorageService storage, IAuditService audit, ICurrentUserService currentUser, CancellationToken ct = default);

    /// <summary>
    /// Replace stored file bytes for an existing document (Word/Spreadsheet editor save-back).
    /// Updates size, optional plain-text extraction, clears document embedding chunks, audits Update.
    /// </summary>
    Task<Document> ReplaceContentAsync(
        Guid id,
        Stream content,
        string? contentType,
        long length,
        IFileStorageService storage,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default);

    /// <summary>
    /// Update clerk-facing metadata (rename / move between AI folders) for File Manager Browse mode.
    /// </summary>
    Task<Document> UpdateMetadataAsync(
        Guid id,
        string? fileName,
        string? suggestedFolder,
        bool updateFolder,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default);

    /// <summary>Soft-delete (recycle bin). Prefer over hard delete for clerk recovery.</summary>
    Task SoftDeleteAsync(Guid id, IAuditService audit, ICurrentUserService currentUser, CancellationToken ct = default);

    /// <summary>Restore a soft-deleted document to the active library.</summary>
    Task RestoreAsync(Guid id, IAuditService audit, ICurrentUserService currentUser, CancellationToken ct = default);

    /// <summary>Permanently remove soft-deleted document + storage (and versions).</summary>
    Task PurgeAsync(Guid id, IFileStorageService storage, IAuditService audit, ICurrentUserService currentUser, CancellationToken ct = default);

    Task<IReadOnlyList<DocumentVersion>> ListVersionsAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>Restore a prior version as the current content (creates a new version of the current bytes first).</summary>
    Task<Document> RestoreVersionAsync(
        Guid documentId,
        Guid versionId,
        IFileStorageService storage,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default);
}
