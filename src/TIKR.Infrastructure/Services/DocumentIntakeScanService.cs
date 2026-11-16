using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Configuration;
using TIKR.Shared.Diagnostics;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;
using TIKR.Shared.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

public sealed class DocumentIntakeScanService(
    TikrDbContext db,
    IFileStorageService storage,
    IDocumentAgentExtractionBackend extractionBackend,
    IOllamaChatClientFactory ollamaFactory,
    GrokService grokService,
    FeatureSettingsState featureSettings,
    IConfiguration configuration,
    IContactService contacts,
    IRequirementService requirements,
    ILogger<DocumentIntakeScanService> logger) : IDocumentIntakeScanService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<DocumentIntakeBriefDto> ScanAsync(Guid documentId, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        TikrActionLog.Started(logger, "AI.DocumentIntake.Scan", $"DocumentId={documentId}");

        var document = await db.Documents.FindAsync([documentId], ct)
            ?? throw new KeyNotFoundException($"Document {documentId} not found.");
        if (document.DeletedAt is not null)
            throw new InvalidOperationException("Cannot scan a deleted document. Restore it first.");

        await TryBackfillFullTextAsync(document, ct);

        var text = document.FullTextContent ?? document.FileName;
        var heuristic = DueOutFieldParser.Parse(text);
        var payload = SeedFromHeuristic(heuristic);

        var localJson = await TryLocalJsonAsync(document.FileName, text, ct);
        if (localJson is not null)
            payload = MergePayloads(payload, localJson);

        var usedGrok = false;
        var snap = featureSettings.Current;
        if (snap.GrokEnabled && snap.UseGrokForDocumentIntake)
        {
            var redacted = CloudOutboundGuard.Redact(
                text.Length <= DocumentIntakePromptBuilder.MaxExcerptChars
                    ? text
                    : text[..DocumentIntakePromptBuilder.MaxExcerptChars]);
            var grokPrompt = DocumentIntakePromptBuilder.Build(document.FileName, redacted);
            var grokRaw = await grokService.CompleteAsync(grokPrompt, cancellationToken: ct);
            var grokPayload = ParseModelJson(grokRaw);
            if (grokPayload is not null)
            {
                payload = MergePayloadsPreferringLocalContacts(payload, grokPayload);
                usedGrok = true;
            }
        }

        // Dismiss prior pending briefs for this document.
        var prior = await db.DocumentIntakeBriefs
            .Where(b => b.DocumentId == documentId && b.Status == DocumentIntakeBriefStatus.Pending)
            .ToListAsync(ct);
        foreach (var p in prior)
            p.Status = DocumentIntakeBriefStatus.Dismissed;

        var entity = new DocumentIntakeBrief
        {
            Id = Guid.NewGuid(),
            DocumentId = documentId,
            Status = DocumentIntakeBriefStatus.Pending,
            JsonPayload = JsonSerializer.Serialize(payload, JsonOptions),
            ProcessedLocally = !usedGrok || true,
            UsedGrok = usedGrok,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.DocumentIntakeBriefs.Add(entity);
        await db.SaveChangesAsync(ct);

        TikrActionLog.Completed(logger, "AI.DocumentIntake.Scan",
            $"DocumentId={documentId} Due={payload.DueDates.Count} Contacts={payload.Contacts.Count} Actions={payload.ActionItems.Count} UsedGrok={usedGrok}",
            sw.ElapsedMilliseconds);

        return ToDto(entity, document.FileName, payload);
    }

    public async Task<DocumentIntakeBriefDto?> GetPendingAsync(Guid documentId, CancellationToken ct = default)
    {
        var brief = await db.DocumentIntakeBriefs
            .Where(b => b.DocumentId == documentId && b.Status == DocumentIntakeBriefStatus.Pending)
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (brief is null)
            return null;

        var doc = await db.Documents.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);
        var payload = DeserializePayload(brief.JsonPayload);
        return ToDto(brief, doc?.FileName ?? "document", payload);
    }

    public async Task<AcceptDocumentIntakeBriefResponse> AcceptAsync(
        Guid documentId,
        AcceptDocumentIntakeBriefRequest request,
        IAuditService audit,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        var brief = await db.DocumentIntakeBriefs
            .Where(b => b.DocumentId == documentId && b.Status == DocumentIntakeBriefStatus.Pending)
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("No pending intake brief for this document.");

        var document = await db.Documents.FindAsync([documentId], ct)
            ?? throw new KeyNotFoundException($"Document {documentId} not found.");

        var payload = DeserializePayload(brief.JsonPayload);
        var contactIdx = request.ContactIndexes ?? Enumerable.Range(0, payload.Contacts.Count).ToList();
        var actionIdx = request.ActionItemIndexes ?? Enumerable.Range(0, payload.ActionItems.Count).ToList();
        var dueIdx = request.DueDateIndexes ?? Enumerable.Range(0, payload.DueDates.Count).ToList();

        var contactsUpserted = 0;
        foreach (var i in contactIdx.Distinct().Where(i => i >= 0 && i < payload.Contacts.Count))
        {
            var c = payload.Contacts[i];
            if (string.IsNullOrWhiteSpace(c.Name))
                continue;
            await contacts.UpsertAsync(
                new CreateContactRequest(
                    c.Name.Trim(),
                    c.Role,
                    c.Organization,
                    Email: string.IsNullOrWhiteSpace(c.Email) || c.Email.Contains('[', StringComparison.Ordinal) ? null : c.Email,
                    Phone: string.IsNullOrWhiteSpace(c.Phone) || c.Phone.Contains('[', StringComparison.Ordinal) ? null : c.Phone,
                    Notes: c.Notes ?? $"From document intake: {document.FileName}",
                    Categories: ContactCategory.Custom),
                audit,
                currentUser,
                ct);
            contactsUpserted++;
        }

        var requirementIds = new List<Guid>();
        var primaryContact = contactIdx
            .Where(i => i >= 0 && i < payload.Contacts.Count)
            .Select(i => payload.Contacts[i])
            .FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.Name));

        // Build work items: explicit action items, else due-date-only rows.
        var work = new List<(string Title, DateOnly Due, string? Notes)>();
        foreach (var i in actionIdx.Distinct().Where(i => i >= 0 && i < payload.ActionItems.Count))
        {
            var a = payload.ActionItems[i];
            if (string.IsNullOrWhiteSpace(a.Title))
                continue;
            var due = a.DueDate
                ?? payload.DueDates.ElementAtOrDefault(0)?.Date
                ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14));
            work.Add((a.Title.Trim(), due, a.Notes));
        }

        if (work.Count == 0)
        {
            foreach (var i in dueIdx.Distinct().Where(i => i >= 0 && i < payload.DueDates.Count))
            {
                var d = payload.DueDates[i];
                var title = string.IsNullOrWhiteSpace(d.Label)
                    ? $"Follow-up from {document.FileName}"
                    : d.Label!;
                if (title.Length > 500)
                    title = title[..500];
                work.Add((title, d.Date, d.SourceSnippet));
            }
        }

        if (work.Count == 1)
        {
            var (title, due, notes) = work[0];
            var req = await requirements.CreateAsync(
                new CreateRequirementRequest(
                    title,
                    notes,
                    due,
                    RecurrenceType.None,
                    RequirementCategory.Custom,
                    SubmitTo: primaryContact?.Organization,
                    ContactName: primaryContact?.Name,
                    ContactEmail: PlaceholderOrNull(primaryContact?.Email),
                    ContactPhone: PlaceholderOrNull(primaryContact?.Phone)),
                audit,
                currentUser,
                ct);
            await requirements.LinkDocumentAsync(req.Id, documentId, audit, currentUser, ct);
            requirementIds.Add(req.Id);
        }
        else if (work.Count > 1)
        {
            var primaryDue = work.Min(w => w.Due);
            var primaryTitle = $"Actions from {document.FileName}";
            if (primaryTitle.Length > 500)
                primaryTitle = primaryTitle[..500];
            var req = await requirements.CreateAsync(
                new CreateRequirementRequest(
                    primaryTitle,
                    $"Intake scan created {work.Count} action items.",
                    primaryDue,
                    RecurrenceType.None,
                    RequirementCategory.Custom,
                    SubmitTo: primaryContact?.Organization,
                    ContactName: primaryContact?.Name,
                    ContactEmail: PlaceholderOrNull(primaryContact?.Email),
                    ContactPhone: PlaceholderOrNull(primaryContact?.Phone)),
                audit,
                currentUser,
                ct);
            await requirements.LinkDocumentAsync(req.Id, documentId, audit, currentUser, ct);
            requirementIds.Add(req.Id);

            var order = 0;
            foreach (var (title, due, notes) in work)
            {
                await requirements.AddChecklistItemAsync(
                    req.Id,
                    new CreateRequirementChecklistItemRequest(
                        title,
                        Description: notes,
                        SortOrder: order++,
                        DueOffsetDays: due.DayNumber - primaryDue.DayNumber),
                    audit,
                    currentUser,
                    ct);
            }
        }

        brief.Status = DocumentIntakeBriefStatus.Accepted;
        brief.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        TikrActionLog.Completed(logger, "AI.DocumentIntake.Accept",
            $"DocumentId={documentId} Contacts={contactsUpserted} Requirements={requirementIds.Count}");

        return new AcceptDocumentIntakeBriefResponse(brief.Id, contactsUpserted, requirementIds.Count, requirementIds);
    }

    public async Task DismissAsync(Guid documentId, CancellationToken ct = default)
    {
        var pending = await db.DocumentIntakeBriefs
            .Where(b => b.DocumentId == documentId && b.Status == DocumentIntakeBriefStatus.Pending)
            .ToListAsync(ct);
        foreach (var b in pending)
        {
            b.Status = DocumentIntakeBriefStatus.Dismissed;
            b.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        TikrActionLog.Completed(logger, "AI.DocumentIntake.Dismiss", $"DocumentId={documentId} Count={pending.Count}");
    }

    private async Task TryBackfillFullTextAsync(Document document, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(document.FullTextContent) || string.IsNullOrWhiteSpace(document.StoragePath))
            return;

        try
        {
            await using var stream = await storage.OpenReadAsync(document.StoragePath, ct);
            await using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            buffer.Position = 0;
            var result = await extractionBackend.ExtractAsync(buffer, document.FileName, ct);
            if (string.IsNullOrWhiteSpace(result.ExtractedText)
                || result.ExtractedText.StartsWith("Agent stub:", StringComparison.Ordinal))
                return;
            var text = result.ExtractedText.Trim();
            if (text.Length > 200_000)
                text = text[..200_000];
            document.FullTextContent = text;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Intake backfill failed for {DocumentId}", document.Id);
        }
    }

    private async Task<DocumentIntakeBriefPayload?> TryLocalJsonAsync(
        string fileName,
        string text,
        CancellationToken ct)
    {
        try
        {
            var prompt = DocumentIntakePromptBuilder.Build(fileName, text);
            var timeoutSeconds = TikrConfiguration.GetChatCompletionTimeoutSeconds(configuration);
            using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            attemptCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            var client = ollamaFactory.CreateChatClient();
            var options = new ChatOptions { Temperature = DocumentIntakePromptBuilder.Temperature };
            var response = await client.GetResponseAsync(
                [new ChatMessage(ChatRole.User, prompt)],
                options,
                attemptCts.Token);
            return ParseModelJson(response.Text);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Local intake completion failed for {FileName}", fileName);
            return null;
        }
    }

    internal static DocumentIntakeBriefPayload SeedFromHeuristic(ParsedDueOutFields h)
    {
        var dues = new List<DocumentIntakeDueDateDto>();
        if (h.DueDate is { } d)
            dues.Add(new DocumentIntakeDueDateDto(d, "Due date", null));

        var contacts = new List<DocumentIntakeContactDto>();
        if (!string.IsNullOrWhiteSpace(h.ContactName)
            || !string.IsNullOrWhiteSpace(h.ContactEmail)
            || !string.IsNullOrWhiteSpace(h.ContactPhone))
        {
            contacts.Add(new DocumentIntakeContactDto(
                string.IsNullOrWhiteSpace(h.ContactName) ? (h.ContactEmail ?? "Contact") : h.ContactName!,
                h.SubmitTo,
                h.ContactEmail,
                h.ContactPhone));
        }

        return new DocumentIntakeBriefPayload(dues, contacts, []);
    }

    internal static DocumentIntakeBriefPayload? ParseModelJson(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        try
        {
            var json = ExtractJson(raw);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var dues = new List<DocumentIntakeDueDateDto>();
            if (root.TryGetProperty("dueDates", out var dueEl) && dueEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in dueEl.EnumerateArray())
                {
                    if (!item.TryGetProperty("date", out var dateEl))
                        continue;
                    if (!DateOnly.TryParse(dateEl.GetString(), out var date))
                        continue;
                    dues.Add(new DocumentIntakeDueDateDto(
                        date,
                        item.TryGetProperty("label", out var l) ? l.GetString() : null,
                        item.TryGetProperty("sourceSnippet", out var s) ? s.GetString() : null));
                }
            }

            var contacts = new List<DocumentIntakeContactDto>();
            if (root.TryGetProperty("contacts", out var cEl) && cEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in cEl.EnumerateArray())
                {
                    var name = item.TryGetProperty("name", out var n) ? n.GetString() : null;
                    if (string.IsNullOrWhiteSpace(name))
                        continue;
                    contacts.Add(new DocumentIntakeContactDto(
                        name!,
                        item.TryGetProperty("organization", out var o) ? o.GetString() : null,
                        item.TryGetProperty("email", out var e) ? e.GetString() : null,
                        item.TryGetProperty("phone", out var p) ? p.GetString() : null,
                        item.TryGetProperty("role", out var r) ? r.GetString() : null,
                        item.TryGetProperty("notes", out var notes) ? notes.GetString() : null));
                }
            }

            var actions = new List<DocumentIntakeActionItemDto>();
            if (root.TryGetProperty("actionItems", out var aEl) && aEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in aEl.EnumerateArray())
                {
                    var title = item.TryGetProperty("title", out var t) ? t.GetString() : null;
                    if (string.IsNullOrWhiteSpace(title))
                        continue;
                    DateOnly? due = null;
                    if (item.TryGetProperty("dueDate", out var dd) && dd.ValueKind == JsonValueKind.String
                        && DateOnly.TryParse(dd.GetString(), out var parsed))
                        due = parsed;
                    actions.Add(new DocumentIntakeActionItemDto(
                        title!,
                        due,
                        item.TryGetProperty("notes", out var notes) ? notes.GetString() : null));
                }
            }

            return new DocumentIntakeBriefPayload(dues, contacts, actions);
        }
        catch
        {
            return null;
        }
    }

    internal static DocumentIntakeBriefPayload MergePayloads(
        DocumentIntakeBriefPayload seed,
        DocumentIntakeBriefPayload model)
    {
        var dues = seed.DueDates.Concat(model.DueDates)
            .GroupBy(d => d.Date)
            .Select(g => g.First())
            .ToList();
        var contacts = DedupContacts(seed.Contacts.Concat(model.Contacts));
        var actions = seed.ActionItems.Concat(model.ActionItems)
            .GroupBy(a => a.Title, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
        return new DocumentIntakeBriefPayload(dues, contacts, actions);
    }

    /// <summary>When merging Grok (redacted) into local, keep local emails/phones over placeholders.</summary>
    internal static DocumentIntakeBriefPayload MergePayloadsPreferringLocalContacts(
        DocumentIntakeBriefPayload local,
        DocumentIntakeBriefPayload cloud)
    {
        var merged = MergePayloads(local, cloud);
        var fixedContacts = merged.Contacts.Select(c =>
        {
            var localMatch = local.Contacts.FirstOrDefault(l =>
                string.Equals(l.Name, c.Name, StringComparison.OrdinalIgnoreCase));
            if (localMatch is null)
                return c with
                {
                    Email = PlaceholderOrNull(c.Email),
                    Phone = PlaceholderOrNull(c.Phone)
                };
            return c with
            {
                Email = PreferConcrete(localMatch.Email, c.Email),
                Phone = PreferConcrete(localMatch.Phone, c.Phone),
                Organization = string.IsNullOrWhiteSpace(c.Organization) ? localMatch.Organization : c.Organization
            };
        }).ToList();
        return merged with { Contacts = fixedContacts };
    }

    private static List<DocumentIntakeContactDto> DedupContacts(IEnumerable<DocumentIntakeContactDto> contacts) =>
        contacts
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .GroupBy(c => c.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

    private static string? PreferConcrete(string? local, string? cloud)
    {
        if (!string.IsNullOrWhiteSpace(local) && !local.Contains('[', StringComparison.Ordinal))
            return local;
        return PlaceholderOrNull(cloud);
    }

    private static string? PlaceholderOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Contains('[', StringComparison.Ordinal) ? null : value;

    private static string ExtractJson(string raw)
    {
        var start = raw.IndexOf('{');
        var end = raw.LastIndexOf('}');
        if (start >= 0 && end > start)
            return raw[start..(end + 1)];
        return raw;
    }

    private static DocumentIntakeBriefPayload DeserializePayload(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<DocumentIntakeBriefPayload>(json, JsonOptions)
                   ?? new DocumentIntakeBriefPayload([], [], []);
        }
        catch
        {
            return new DocumentIntakeBriefPayload([], [], []);
        }
    }

    private static DocumentIntakeBriefDto ToDto(
        DocumentIntakeBrief entity,
        string fileName,
        DocumentIntakeBriefPayload payload) =>
        new(
            entity.Id,
            entity.DocumentId,
            fileName,
            entity.Status.ToString(),
            payload.DueDates,
            payload.Contacts,
            payload.ActionItems,
            entity.ProcessedLocally,
            entity.UsedGrok,
            entity.CreatedAt);
}
