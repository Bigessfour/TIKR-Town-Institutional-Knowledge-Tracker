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
        CancellationToken ct = default);

    // Future: other CRUD if needed beyond thin endpoints; queries can stay direct for now.
}
