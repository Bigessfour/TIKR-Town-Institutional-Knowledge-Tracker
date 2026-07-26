using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TIKR.Shared.Configuration;
using TIKR.Shared.DTOs;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// Watches a local folder (forward-to-folder / IMAP drop) and uploads new files as Documents.
/// Configure with <c>TIKR_EMAIL_INBOX_PATH</c>. Processed files move to <c>processed/</c> under that path.
/// </summary>
public sealed class FolderEmailIngestionService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<FolderEmailIngestionService> logger) : IEmailIngestionService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".txt", ".md", ".csv", ".eml", ".msg"
    };

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(TikrConfiguration.GetEmailInboxPath(configuration));

    public async Task<EmailIngestionResult> IngestPendingAsync(CancellationToken ct = default)
    {
        var inboxPath = TikrConfiguration.GetEmailInboxPath(configuration);
        if (string.IsNullOrWhiteSpace(inboxPath))
            return new EmailIngestionResult(0, 0, ["Email inbox path is not configured (TIKR_EMAIL_INBOX_PATH)."]);

        Directory.CreateDirectory(inboxPath);
        var processedDir = Path.Combine(inboxPath, "processed");
        Directory.CreateDirectory(processedDir);

        var errors = new List<string>();
        var ingested = 0;
        var skipped = 0;

        foreach (var filePath in Directory.EnumerateFiles(inboxPath))
        {
            ct.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(filePath);
            var ext = Path.GetExtension(fileName);
            if (!AllowedExtensions.Contains(ext))
            {
                skipped++;
                continue;
            }

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var documents = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
                var audit = scope.ServiceProvider.GetRequiredService<IAuditService>();
                var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();

                await using var stream = File.OpenRead(filePath);
                var contentType = GuessContentType(ext);
                await documents.UploadAsync(
                    stream,
                    fileName,
                    contentType,
                    stream.Length,
                    storage,
                    audit,
                    currentUser,
                    ct);

                var dest = Path.Combine(processedDir, $"{DateTime.UtcNow:yyyyMMddHHmmss}_{fileName}");
                File.Move(filePath, dest, overwrite: true);
                ingested++;
                logger.LogInformation("Ingested email-folder file {FileName} as document", fileName);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to ingest email-folder file {FileName}", fileName);
                errors.Add($"{fileName}: {ex.Message}");
            }
        }

        return new EmailIngestionResult(ingested, skipped, errors);
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
