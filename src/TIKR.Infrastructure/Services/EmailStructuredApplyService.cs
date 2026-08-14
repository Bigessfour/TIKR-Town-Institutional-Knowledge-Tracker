using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TIKR.Shared.Diagnostics;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
using TIKR.Shared.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

public sealed class EmailStructuredApplyService(
    IContactService contacts,
    IKnowledgeService knowledge,
    IHybridAiService ai,
    IEmailIngestionNoticeStore notices,
    ILogger<EmailStructuredApplyService>? logger = null) : IEmailStructuredApplyService
{
    private readonly ILogger _log = logger ?? NullLogger<EmailStructuredApplyService>.Instance;

    public async Task<EmailExtractionNoticeDto> ApplyAsync(
        EmailStructuredExtractResult extract,
        string fileName,
        Guid? documentId,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        TikrActionLog.Started(_log, "Email.StructuredApply",
            $"FileName={fileName} DocumentId={documentId} ParseOk={extract.Succeeded} Election={extract.IsElectionRelated}");

        var created = 0;
        var updated = 0;
        var names = new List<string>();
        Guid? knowledgeId = null;
        EmailRequirementSuggestionDto? suggestion = null;
        string? message;

        try
        {
            if (!extract.Succeeded)
            {
                message = $"Parse failed for {fileName}: {extract.Error}";
                TikrActionLog.Failed(_log, "Email.StructuredApply", extract.Error ?? "parse failed",
                    $"FileName={fileName}");
                var failNotice = new EmailExtractionNoticeDto(
                    DateTime.UtcNow, fileName, documentId, [], 0, 0, null, null, message, false);
                notices.Add(failNotice);
                return failNotice;
            }

            foreach (var c in extract.Contacts)
            {
                ct.ThrowIfCancellationRequested();
                var req = new CreateContactRequest(
                    c.Name,
                    c.Role,
                    c.Organization,
                    Address: null,
                    Office: null,
                    c.Email,
                    c.Phone,
                    c.Notes,
                    c.Categories);

                var (entity, wasCreated) = await contacts.UpsertAsync(req, audit, currentUser, ct);
                names.Add(entity.Name);
                if (wasCreated) created++;
                else updated++;

                _log.LogInformation(
                    "Email extract contact {Phase} ContactId={ContactId} Category={Category} FileName={FileName} UserId={UserId}",
                    wasCreated ? "created" : "updated",
                    entity.Id,
                    entity.Categories,
                    fileName,
                    currentUser.UserId);
            }

            if (extract.Contacts.Count > 0 || extract.IsElectionRelated)
            {
                var title = !string.IsNullOrWhiteSpace(extract.Subject)
                    ? $"Email: {extract.Subject}"
                    : $"Email contact note ({fileName})";
                if (title.Length > 500)
                    title = title[..500];

                var content = BuildKnowledgeContent(extract, fileName);
                var entry = await knowledge.CreateAsync(
                    new CreateKnowledgeEntryRequest(title, content, KnowledgeCategory.Contact, SortOrder: 0),
                    audit,
                    ai,
                    currentUser,
                    ct);
                knowledgeId = entry.Id;
            }

            if (extract.RequirementSuggestion is { } hint)
            {
                suggestion = new EmailRequirementSuggestionDto(
                    hint.Title,
                    hint.DueDate,
                    hint.SubmitTo,
                    hint.ContactName,
                    hint.ContactEmail,
                    hint.ContactPhone,
                    fileName,
                    documentId);
            }

            message = names.Count > 0
                ? $"Extracted contact(s) from email {fileName}; review in Vault Contacts"
                : extract.IsElectionRelated
                    ? $"Election-related email {fileName} ingested; review Vault / suggestion"
                    : $"Email {fileName} ingested; no contact fields found";

            var notice = new EmailExtractionNoticeDto(
                DateTime.UtcNow,
                fileName,
                documentId,
                names,
                created,
                updated,
                knowledgeId,
                suggestion,
                message,
                true);

            notices.Add(notice);
            TikrActionLog.Completed(_log, "Email.StructuredApply",
                $"FileName={fileName} ContactsCreated={created} ContactsUpdated={updated} KnowledgeId={knowledgeId} UserId={currentUser.UserId}");
            return notice;
        }
        catch (Exception ex)
        {
            TikrActionLog.Failed(_log, "Email.StructuredApply", ex, $"FileName={fileName}");
            var err = new EmailExtractionNoticeDto(
                DateTime.UtcNow,
                fileName,
                documentId,
                names,
                created,
                updated,
                knowledgeId,
                suggestion,
                $"Extract error for {fileName}: {ex.Message}",
                extract.Succeeded);
            notices.Add(err);
            return err;
        }
    }

    private static string BuildKnowledgeContent(EmailStructuredExtractResult extract, string fileName)
    {
        var lines = new List<string>
        {
            $"Source file: {fileName}",
            $"From: {extract.From}",
            $"Reply-To: {extract.ReplyTo}",
            $"Subject: {extract.Subject}",
            $"Election-related: {extract.IsElectionRelated}",
            "",
            extract.BodyPreview ?? ""
        };
        return string.Join('\n', lines).Trim();
    }
}
