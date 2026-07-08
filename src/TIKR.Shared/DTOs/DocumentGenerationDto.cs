namespace TIKR.Shared.DTOs;

public record GeneratedDocumentResult(byte[] Content, string FileName, string ContentType);

public record CouncilAgendaItem(string Title, string? Description, DateOnly? DueDate);

public record CouncilAgendaRequest(
    string TownName,
    DateOnly MeetingDate,
    IReadOnlyList<CouncilAgendaItem> Items);

public record MeetingMinutesRequest(
    string TownName,
    DateOnly MeetingDate,
    string? BoardName,
    IReadOnlyList<string>? Attendees,
    IReadOnlyList<string>? AgendaItems,
    string? Notes);

public record ClerkMemoRequest(
    string TownName,
    string Subject,
    string Body,
    string? Recipient,
    DateOnly? MemoDate);

public record ComplianceReportRow(
    string Title,
    string? Description,
    DateOnly DueDate,
    string Category,
    bool IsCompleted);

public record ComplianceReportRequest(
    string TownName,
    DateOnly ReportDate,
    IReadOnlyList<ComplianceReportRow> Rows);
