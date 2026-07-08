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

public record GeneratedDocumentDownloadDto(byte[] Content, string FileName, string ContentType);

public record DocumentGenerationResponse(
    GeneratedDocumentDownloadDto? Document,
    string? ErrorMessage);

/// <summary>
/// Result from on-demand Document SDK text/tables extraction for a stored document.
/// Used by Documents.razor "Extract Text/Tables to Vault" feature.
/// </summary>
public record DocumentTextExtractResult(string? ExtractedText, int TablesExtractedCount);

public record CouncilPacketLinkedDocument(
    Guid DocumentId,
    string FileName,
    string? Summary);

/// <summary>
/// Request for generating the complete Vault Handover Package PDF.
/// Data is collected server-side for one-click generation.
/// </summary>
public record HandoverPackageRequest(
    string TownName,
    DateTime GeneratedAt,
    IReadOnlyList<KnowledgeEntryDto> KnowledgeEntries,
    IReadOnlyList<RequirementDto> Requirements,
    IReadOnlyList<DocumentDto> Documents,
    IReadOnlyList<CalendarSnapshotItem> CalendarSnapshot);

public record CalendarSnapshotItem(string Title, DateOnly DueDate, string? Category);

public record CouncilPacketRequirementItem(
    Guid Id,
    string Title,
    string? Description,
    DateOnly DueDate,
    string Category,
    string Status,
    string Urgency,
    bool IsCompleted,
    IReadOnlyList<CouncilPacketLinkedDocument> LinkedDocuments);

public record CreateCouncilPacketRequest(
    string TownName,
    DateOnly PacketDate,
    string? LogoPath,
    IReadOnlyList<CouncilPacketRequirementItem> Requirements);

public record CouncilPacketGeneratedFiles(
    byte[] PdfContent,
    string PdfFileName,
    byte[] DocxContent,
    string DocxFileName);

public record CouncilPacketStoredFileDto(
    Guid DocumentId,
    string FileName,
    string DownloadUrl);

public record CouncilPacketResponse(
    CouncilPacketStoredFileDto? Pdf,
    CouncilPacketStoredFileDto? Docx,
    string? ErrorMessage);
