using System.Text.Json;
using Syncfusion.AI.AgentTools.Core;
using Syncfusion.AI.AgentTools.DataExtraction;
using Syncfusion.AI.AgentTools.OfficeToPDF;
using Syncfusion.AI.AgentTools.PDF;
using Syncfusion.AI.AgentTools.PowerPoint;
using Syncfusion.AI.AgentTools.Word;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// Deterministic Syncfusion Storage Mode extraction for Requirements AI Scan (Phase 10C-A2).
/// Orchestrated tool selection via <see cref="SyncfusionDocumentAgentOrchestrator"/> when enabled (A3).
/// </summary>
public sealed class SyncfusionDocumentAgentExtractor
{
    private readonly NasSyncfusionDocumentStorage _storage;
    private readonly DocumentStorageManager _manager;
    private readonly PdfContentExtractionAgentTools _pdfTools;
    private readonly WordImportExportAgentTools _wordTools;
    private readonly DataExtractionAgentTools _dataTools;
    private readonly PresentationContentAgentTools _pptTools;
    private readonly OfficeToPdfAgentTools _officeToPdf;
    private readonly SyncfusionDocumentAgentOrchestrator _orchestrator;

    public SyncfusionDocumentAgentExtractor(
        NasSyncfusionDocumentStorage storage,
        SyncfusionDocumentAgentOrchestrator orchestrator)
    {
        _storage = storage;
        _orchestrator = orchestrator;
        _manager = new DocumentStorageManager(storage);
        _pdfTools = new PdfContentExtractionAgentTools(_manager);
        _wordTools = new WordImportExportAgentTools(_manager);
        _dataTools = new DataExtractionAgentTools(_manager);
        _pptTools = new PresentationContentAgentTools(_manager);
        _officeToPdf = new OfficeToPdfAgentTools(_manager);
    }

    public async Task<AgentExtractionResult> ExtractAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        if (DocumentTextExtractionService.CanExtract(fileName))
        {
            var plain = await DocumentTextExtractionService.TryExtractAsync(buffer, fileName, cancellationToken);
            if (!string.IsNullOrWhiteSpace(plain))
            {
                return new AgentExtractionResult(
                    plain,
                    DocumentAgentService.InferTableCount(fileName),
                    UsedSyncfusionTools: false);
            }
        }

        buffer.Position = 0;
        var workName = $"{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
        _storage.Write(workName, buffer);

        var orchestrated = await _orchestrator.TryExtractAsync(workName, fileName, cancellationToken);
        if (orchestrated is not null)
            return orchestrated;

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => ExtractPdf(workName, fileName),
            ".doc" or ".docx" => ExtractWord(workName),
            ".xls" or ".xlsx" => ExtractExcel(workName, fileName),
            ".ppt" or ".pptx" => ExtractPowerPoint(workName),
            _ => new AgentExtractionResult(
                $"Syncfusion AgentTools: unsupported type {ext}. Upload PDF, Word, Excel, PowerPoint, or plain text.",
                0,
                UsedSyncfusionTools: true)
        };
    }

    private AgentExtractionResult ExtractPdf(string workName, string originalFileName)
    {
        var text = UnwrapPayload(_pdfTools.ExtractText(workName, startPageIndex: 0, endPageIndex: -1));
        var tables = TryCountTables(workName, originalFileName);
        return new AgentExtractionResult(text, tables, UsedSyncfusionTools: true);
    }

    private AgentExtractionResult ExtractWord(string workName)
    {
        var text = UnwrapPayload(_wordTools.GetText(workName));
        return new AgentExtractionResult(text, 1, UsedSyncfusionTools: true);
    }

    private AgentExtractionResult ExtractExcel(string workName, string originalFileName)
    {
        var pdfPath = Path.ChangeExtension(workName, ".pdf");
        var convertResult = _officeToPdf.ConvertToPdf(workName, "Excel", pdfPath);
        var resolvedPdf = ResolveConvertedPdfPath(convertResult, workName) ?? pdfPath;
        if (_storage.Exists(resolvedPdf))
        {
            var text = UnwrapPayload(_pdfTools.ExtractText(resolvedPdf, startPageIndex: 0, endPageIndex: -1));
            if (!string.IsNullOrWhiteSpace(text))
                return new AgentExtractionResult(text, DocumentAgentService.InferTableCount(originalFileName), UsedSyncfusionTools: true);
        }

        var fallback = UnwrapPayload(convertResult);
        return new AgentExtractionResult(
            string.IsNullOrWhiteSpace(fallback)
                ? $"Syncfusion AgentTools: could not extract Excel content from {Path.GetFileName(originalFileName)}."
                : fallback,
            DocumentAgentService.InferTableCount(originalFileName),
            UsedSyncfusionTools: true);
    }

    private AgentExtractionResult ExtractPowerPoint(string workName)
    {
        var text = UnwrapPayload(_pptTools.GetText(workName));
        return new AgentExtractionResult(text, 1, UsedSyncfusionTools: true);
    }

    private string? ResolveConvertedPdfPath(AgentToolResult result, string sourceWorkName)
    {
        if (result.Success)
        {
            if (result.Data is string dataPath && !string.IsNullOrWhiteSpace(dataPath))
                return dataPath;

            var message = result.Message ?? result.Data?.ToString();
            if (!string.IsNullOrWhiteSpace(message) &&
                message.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return message;
        }

        var sibling = Path.ChangeExtension(sourceWorkName, ".pdf");
        return _storage.Exists(sibling) ? sibling : null;
    }

    private int TryCountTables(string workName, string originalFileName)
    {
        try
        {
            var result = _dataTools.ExtractTableAsJson(
                workName,
                detectBorderlessTables: true,
                confidenceThreshold: 0.5,
                startPage: -1,
                endPage: -1,
                outputFilePath: string.Empty);
            var json = UnwrapPayload(result);
            if (string.IsNullOrWhiteSpace(json))
                return DocumentAgentService.InferTableCount(originalFileName);

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                return doc.RootElement.GetArrayLength();

            if (doc.RootElement.TryGetProperty("tables", out var tables) && tables.ValueKind == JsonValueKind.Array)
                return tables.GetArrayLength();
        }
        catch
        {
            // Best-effort — clerk still gets text.
        }

        return DocumentAgentService.InferTableCount(originalFileName);
    }

    private static string UnwrapPayload(AgentToolResult result)
    {
        if (!result.Success)
            return result.Error ?? string.Empty;

        var dataText = result.Data?.ToString();
        if (!string.IsNullOrWhiteSpace(dataText) &&
            !dataText.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return dataText;

        return result.Message ?? dataText ?? string.Empty;
    }
}
