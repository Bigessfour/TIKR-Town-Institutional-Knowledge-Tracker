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

    Task<CouncilPacketGeneratedFiles> GenerateCouncilPacketAsync(
        CreateCouncilPacketRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a clean, tagged PDF archive copy of an uploaded document (any supported format).
    /// Converts non-PDFs, adds "AI Processed - [Date] - TIKR Vault" stamp + metadata.
    /// Used by agent scan archive extension (Grok Heavy recommended).
    /// </summary>
    Task<GeneratedDocumentResult> CreateAgentArchivePdfAsync(
        Stream content,
        string fileName,
        DateTime processedDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts an image (png/jpg/etc) to PDF using Syncfusion Document SDK.
    /// Supports the "Convert to PDF" and on-the-fly preview features in Documents.razor.
    /// </summary>
    Task<GeneratedDocumentResult> ConvertImageToPdfAsync(
        Stream imageContent,
        string fileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates the complete searchable handover package PDF for the Vault.
    /// Includes Knowledge + Voice, Requirements, Documents, Calendar snapshot, with TOC and bookmarks.
    /// </summary>
    Task<GeneratedDocumentResult> GenerateHandoverPackagePdfAsync(
        HandoverPackageRequest request,
        CancellationToken cancellationToken = default);
}
