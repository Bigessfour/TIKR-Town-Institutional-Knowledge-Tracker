using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TIKR.Shared.Configuration;
using TIKR.Shared.Diagnostics;
using TIKR.Shared.DTOs;
using TIKR.Shared.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// Watches a local folder (forward-to-folder / IMAP drop) and uploads new files as Documents.
/// Optionally runs structured email extract (contacts / knowledge / suggestions).
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
        var notices = new List<EmailExtractionNoticeDto>();
        var ingested = 0;
        var skipped = 0;
        var contactsCreated = 0;
        var contactsUpdated = 0;
        var knowledgeCreated = 0;
        var extractEnabled = TikrConfiguration.GetEmailStructuredExtractEnabled(configuration);

        TikrActionLog.Started(logger, "Email.FolderIngest",
            $"Inbox={inboxPath} StructuredExtract={extractEnabled}");

        foreach (var filePath in Directory.EnumerateFiles(inboxPath))
        {
            ct.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(filePath);
            var ext = Path.GetExtension(fileName);
            if (!AllowedExtensions.Contains(ext))
            {
                skipped++;
                logger.LogInformation("Email folder skip unsupported extension FileName={FileName}", fileName);
                continue;
            }

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var documents = scope.ServiceProvider.GetRequiredService<IDocumentService>();
                var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
                var audit = scope.ServiceProvider.GetRequiredService<IAuditService>();
                var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();

                byte[] bytes;
                await using (var read = File.OpenRead(filePath))
                {
                    using var ms = new MemoryStream();
                    await read.CopyToAsync(ms, ct);
                    bytes = ms.ToArray();
                }

                await using var stream = new MemoryStream(bytes);
                var contentType = GuessContentType(ext);
                var document = await documents.UploadAsync(
                    stream,
                    fileName,
                    contentType,
                    bytes.LongLength,
                    storage,
                    audit,
                    currentUser,
                    ct);

                if (extractEnabled && IsEmailLike(ext))
                {
                    try
                    {
                        var apply = scope.ServiceProvider.GetRequiredService<IEmailStructuredApplyService>();
                        var extract = EmailStructuredExtractor.ParseBytes(bytes, fileName);
                        logger.LogInformation(
                            "Email parse FileName={FileName} Success={Success} ContactCount={ContactCount} Election={Election} Fields={Fields}",
                            fileName,
                            extract.Succeeded,
                            extract.Contacts.Count,
                            extract.IsElectionRelated,
                            $"From={extract.From};Subject={extract.Subject}");

                        var notice = await apply.ApplyAsync(extract, fileName, document.Id, audit, currentUser, ct);
                        notices.Add(notice);
                        contactsCreated += notice.ContactsCreated;
                        contactsUpdated += notice.ContactsUpdated;
                        if (notice.KnowledgeEntryId is not null)
                            knowledgeCreated++;
                    }
                    catch (Exception ex)
                    {
                        // Never fail the ingest because extract blew up.
                        logger.LogWarning(ex, "Structured extract failed after ingest FileName={FileName}", fileName);
                        errors.Add($"{fileName}: extract {ex.Message}");
                    }
                }

                var dest = Path.Combine(processedDir, $"{DateTime.UtcNow:yyyyMMddHHmmss}_{fileName}");
                File.Move(filePath, dest, overwrite: true);
                ingested++;
                logger.LogInformation(
                    "Ingested email-folder file FileName={FileName} DocumentId={DocumentId}",
                    fileName,
                    document.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to ingest email-folder file FileName={FileName}", fileName);
                errors.Add($"{fileName}: {ex.Message}");
            }
        }

        TikrActionLog.Completed(logger, "Email.FolderIngest",
            $"Ingested={ingested} Skipped={skipped} Errors={errors.Count} ContactsCreated={contactsCreated} ContactsUpdated={contactsUpdated} KnowledgeCreated={knowledgeCreated}");

        return new EmailIngestionResult(
            ingested,
            skipped,
            errors,
            contactsCreated,
            contactsUpdated,
            knowledgeCreated,
            notices);
    }

    private static bool IsEmailLike(string ext) =>
        ext.Equals(".eml", StringComparison.OrdinalIgnoreCase) ||
        ext.Equals(".msg", StringComparison.OrdinalIgnoreCase) ||
        ext.Equals(".txt", StringComparison.OrdinalIgnoreCase);

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
