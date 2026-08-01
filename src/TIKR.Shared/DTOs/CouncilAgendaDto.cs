namespace TIKR.Shared.DTOs;

public record CouncilAgendaSection(
    string SectionKey,
    string Title,
    IReadOnlyList<CouncilAgendaItem> Items,
    string? Notes = null);

public record CouncilAgendaBuilderPreview(
    DateOnly MeetingDate,
    string Board,
    string BoardDisplayName,
    DateOnly PriorMeetingDate,
    IReadOnlyList<CouncilAgendaSection> Sections);

public record UnfinishedBusinessSuggestion(
    string Title,
    string? Rationale,
    Guid? SourceDocumentId,
    string? SourceFileName,
    string? SourceQuote);

public record CouncilAgendaBuilderRequest(
    DateOnly MeetingDate,
    string Board = "TOW");

public record UnfinishedBusinessRequest(
    DateOnly MeetingDate,
    string Board = "TOW");

public record CouncilMinutesBuilderPreview(
    DateOnly MeetingDate,
    string Board,
    string BoardDisplayName,
    Guid? DraftMinutesRequirementId,
    string? DraftMinutesRequirementTitle,
    Guid? PostAgendaRequirementId,
    Guid? ActionedAgendaDocumentId,
    string? ActionedAgendaFileName,
    IReadOnlyList<string> AgendaLines,
    string SuggestedFileName);

public record CouncilMinutesBuilderRequest(
    DateOnly MeetingDate,
    string Board = "TOW");
