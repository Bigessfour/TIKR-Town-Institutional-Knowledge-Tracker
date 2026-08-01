using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure.Data;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;
using TIKR.Shared.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

public sealed class CouncilAgendaBuilderService(
    TikrDbContext db,
    IHybridAiService ai) : ICouncilAgendaBuilderService
{
    public async Task<CouncilAgendaBuilderPreview> BuildPreviewAsync(
        DateOnly meetingDate,
        string board = "TOW",
        CancellationToken cancellationToken = default)
    {
        var normalizedBoard = NormalizeBoard(board);
        var priorMeeting = CouncilMeetingSchedule.PreviousSecondMonday(meetingDate);
        var newBusiness = await LoadNewBusinessItemsAsync(meetingDate, cancellationToken);
        var sections = CouncilAgendaScaffold.CreateOrderOfBusiness(
            normalizedBoard,
            meetingDate,
            priorMeeting,
            newBusiness);

        var suggestions = await SuggestUnfinishedBusinessAsync(meetingDate, normalizedBoard, cancellationToken);
        if (suggestions.Count > 0)
        {
            var items = suggestions
                .Select(s => new CouncilAgendaItem(s.Title, s.SourceQuote, null))
                .ToList();
            sections = CouncilAgendaScaffold.WithSectionItems(sections, "old_business", items);
        }

        return new CouncilAgendaBuilderPreview(
            meetingDate,
            normalizedBoard,
            CouncilAgendaScaffold.BoardDisplayName(normalizedBoard),
            priorMeeting,
            sections);
    }

    public async Task<IReadOnlyList<UnfinishedBusinessSuggestion>> SuggestUnfinishedBusinessAsync(
        DateOnly meetingDate,
        string board = "TOW",
        CancellationToken cancellationToken = default)
    {
        var normalizedBoard = NormalizeBoard(board);
        var priorMeeting = CouncilMeetingSchedule.PreviousSecondMonday(meetingDate);
        var boardToken = normalizedBoard == "WSD" ? "WSD" : "TOW";

        var documents = await db.Documents
            .AsNoTracking()
            .Where(d =>
                d.SuggestedFolder == DocumentTagHeuristics.Minutes
                || d.SuggestedFolder == DocumentTagHeuristics.Agenda
                || EF.Functions.Like(d.FileName, $"%{boardToken}%")
                || EF.Functions.Like(d.FileName, "%MINUTE%"))
            .OrderByDescending(d => d.UploadedAt)
            .Take(40)
            .ToListAsync(cancellationToken);

        var priorDoc = documents.FirstOrDefault(d => MatchesPriorMeeting(d.FileName, priorMeeting, boardToken))
            ?? documents.FirstOrDefault(d =>
                d.SuggestedFolder == DocumentTagHeuristics.Minutes
                && d.FileName.Contains(boardToken, StringComparison.OrdinalIgnoreCase));

        var suggestions = new List<UnfinishedBusinessSuggestion>();

        if (priorDoc?.FullTextContent is { Length: > 0 } text)
        {
            foreach (var (title, quote) in UnfinishedBusinessExtractor.Extract(text))
            {
                suggestions.Add(new UnfinishedBusinessSuggestion(
                    title,
                    $"Carry forward from prior meeting ({priorMeeting:MMMM d, yyyy}).",
                    priorDoc.Id,
                    priorDoc.FileName,
                    quote));
            }
        }

        if (suggestions.Count == 0)
        {
            var query =
                $"unfinished business tabled continued postponed from {boardToken} council meeting {priorMeeting:MMMM d yyyy}";
            var search = await ai.SemanticSearchDocumentsAsync(
                new SemanticSearchRequest(query, TopK: 5, Folder: DocumentTagHeuristics.Minutes),
                cancellationToken);

            if (search is { EmbeddingAvailable: true })
            {
                foreach (var hit in search.Hits)
                {
                    foreach (var (title, quote) in UnfinishedBusinessExtractor.Extract(hit.Snippet, maxItems: 3))
                    {
                        suggestions.Add(new UnfinishedBusinessSuggestion(
                            title,
                            "Suggested from embedded minutes (semantic search).",
                            hit.DocumentId,
                            hit.FileName,
                            quote));
                    }

                    if (suggestions.Count >= 8)
                        break;
                }
            }
        }

        return DeduplicateSuggestions(suggestions);
    }

    private async Task<IReadOnlyList<CouncilAgendaItem>> LoadNewBusinessItemsAsync(
        DateOnly meetingDate,
        CancellationToken cancellationToken)
    {
        var marker = CouncilMeetingCycle.Marker;
        var windowStart = meetingDate.AddDays(-CouncilMeetingSchedule.DefaultAgendaLeadDays - 1);
        var windowEnd = meetingDate.AddDays(1);

        var requirements = await db.Requirements
            .AsNoTracking()
            .Where(r =>
                !r.IsCompleted
                && (r.Description == null || !r.Description.Contains(marker))
                && r.DueDate >= windowStart
                && r.DueDate <= windowEnd)
            .OrderBy(r => r.DueDate)
            .Take(15)
            .ToListAsync(cancellationToken);

        if (requirements.Count == 0)
        {
            requirements = await db.Requirements
                .AsNoTracking()
                .Where(r => !r.IsCompleted && (r.Description == null || !r.Description.Contains(marker)))
                .OrderBy(r => r.DueDate)
                .Take(15)
                .ToListAsync(cancellationToken);
        }

        return requirements
            .Select(r => new CouncilAgendaItem(r.Title, r.Description, r.DueDate))
            .ToList();
    }

    internal static bool MatchesPriorMeeting(string fileName, DateOnly priorMeeting, string boardToken)
    {
        if (!fileName.Contains(boardToken, StringComparison.OrdinalIgnoreCase)
            && !fileName.Contains("MINUTE", StringComparison.OrdinalIgnoreCase))
            return false;

        var month = priorMeeting.ToString("MMMM", System.Globalization.CultureInfo.InvariantCulture);
        if (!fileName.Contains(month, StringComparison.OrdinalIgnoreCase))
            return false;

        var day = priorMeeting.Day.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var index = fileName.IndexOf(month, StringComparison.OrdinalIgnoreCase);
        var afterMonth = index >= 0 ? fileName[(index + month.Length)..] : fileName;
        return afterMonth.Contains($" {day}", StringComparison.Ordinal)
               || afterMonth.Contains($" {day:D2}", StringComparison.Ordinal)
               || afterMonth.StartsWith($" {day}", StringComparison.Ordinal);
    }

    internal static string NormalizeBoard(string board) =>
        board.Equals("WSD", StringComparison.OrdinalIgnoreCase) ? "WSD" : "TOW";

    private static IReadOnlyList<UnfinishedBusinessSuggestion> DeduplicateSuggestions(
        List<UnfinishedBusinessSuggestion> suggestions)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<UnfinishedBusinessSuggestion>();
        foreach (var s in suggestions)
        {
            if (seen.Add(s.Title))
                result.Add(s);
            if (result.Count >= 8)
                break;
        }

        return result;
    }

    public async Task<CouncilMinutesBuilderPreview> BuildMinutesPreviewAsync(
        DateOnly meetingDate,
        string board = "TOW",
        CancellationToken cancellationToken = default)
    {
        var normalizedBoard = NormalizeBoard(board);
        var cycleRequirements = await db.Requirements
            .AsNoTracking()
            .Where(r => r.Description != null && r.Description.Contains(CouncilMeetingCycle.Marker))
            .ToListAsync(cancellationToken);

        Requirement? draftMinutes = null;
        Requirement? postAgenda = null;
        foreach (var requirement in cycleRequirements)
        {
            if (CouncilMeetingCycleHelper.Matches(
                    requirement.Description, normalizedBoard, meetingDate, CouncilMeetingCycle.KindDraftMinutes))
                draftMinutes = requirement;
            else if (CouncilMeetingCycleHelper.Matches(
                         requirement.Description, normalizedBoard, meetingDate, CouncilMeetingCycle.KindPostAgenda))
                postAgenda = requirement;
        }

        Guid? agendaDocumentId = null;
        string? agendaFileName = null;
        string? linkedAgendaText = null;
        if (postAgenda is not null)
        {
            var link = await db.RequirementDocuments
                .AsNoTracking()
                .Include(rd => rd.Document)
                .Where(rd => rd.RequirementId == postAgenda.Id)
                .OrderByDescending(rd => rd.LinkedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (link?.Document is not null)
            {
                agendaDocumentId = link.DocumentId;
                agendaFileName = link.Document.FileName;
                linkedAgendaText = link.Document.FullTextContent;
            }
        }

        IReadOnlyList<string> agendaLines;
        if (ActionedAgendaLineExtractor.Extract(linkedAgendaText) is { Count: > 0 } fromLinked)
        {
            agendaLines = fromLinked;
        }
        else
        {
            var priorMeeting = CouncilMeetingSchedule.PreviousSecondMonday(meetingDate);
            var newBusiness = await LoadNewBusinessItemsAsync(meetingDate, cancellationToken);
            var sections = CouncilAgendaScaffold.CreateOrderOfBusiness(
                normalizedBoard,
                meetingDate,
                priorMeeting,
                newBusiness);
            agendaLines = CouncilMinutesFileNaming.FlattenAgendaSections(sections);
        }

        if (agendaLines.Count == 0 && postAgenda is not null)
        {
            agendaLines =
            [
                $"Minutes for {CouncilAgendaScaffold.BoardDisplayName(normalizedBoard)} — {meetingDate:MMMM d, yyyy}"
            ];
        }

        return new CouncilMinutesBuilderPreview(
            meetingDate,
            normalizedBoard,
            CouncilAgendaScaffold.BoardDisplayName(normalizedBoard),
            draftMinutes?.Id,
            draftMinutes?.Title,
            postAgenda?.Id,
            agendaDocumentId,
            agendaFileName,
            agendaLines,
            CouncilMinutesFileNaming.SuggestFileName(meetingDate, normalizedBoard));
    }
}
