using TIKR.Shared.DTOs;

namespace TIKR.Shared.Interfaces;

/// <summary>
/// Server-side municipal document generation via Syncfusion Document SDK (PDF, Word, Excel).
/// Requires <c>SYNCFUSION_LICENSE_KEY</c> — registered at API startup before the HTTP pipeline runs.
/// </summary>
public interface IDocumentGenerationService
{
    Task<GeneratedDocumentResult> GenerateCouncilAgendaPdfAsync(
        CouncilAgendaRequest request,
        CancellationToken cancellationToken = default);

    Task<GeneratedDocumentResult> GenerateMeetingMinutesDocxAsync(
        MeetingMinutesRequest request,
        CancellationToken cancellationToken = default);

    Task<GeneratedDocumentResult> GenerateClerkMemoDocxAsync(
        ClerkMemoRequest request,
        CancellationToken cancellationToken = default);

    Task<GeneratedDocumentResult> GenerateComplianceReportXlsxAsync(
        ComplianceReportRequest request,
        CancellationToken cancellationToken = default);

    Task<GeneratedDocumentResult> ConvertWordToPdfAsync(
        Stream wordContent,
        string fileName,
        CancellationToken cancellationToken = default);

    Task<GeneratedDocumentResult> ConvertExcelToPdfAsync(
        Stream excelContent,
        string fileName,
        CancellationToken cancellationToken = default);
}
