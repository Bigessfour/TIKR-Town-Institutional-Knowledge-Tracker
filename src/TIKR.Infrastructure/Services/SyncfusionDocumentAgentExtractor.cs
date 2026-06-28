using System.Text.Json;
using Syncfusion.AI.AgentTools.Core;
using Syncfusion.AI.AgentTools.DataExtraction;
using Syncfusion.AI.AgentTools.PDF;
using Syncfusion.AI.AgentTools.Word;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// Deterministic Syncfusion Storage Mode extraction for Requirements AI Scan (Phase 10C-A2).
/// Full Microsoft Agent Framework orchestration lands in A3.
/// </summary>
public sealed class SyncfusionDocumentAgentExtractor
{
    private readonly NasSyncfusionDocumentStorage _storage;
    private readonly DocumentStorageManager _manager;
    private readonly PdfContentExtractionAgentTools _pdfTools;
    private readonly WordImportExportAgentTools _wordTools;
    private readonly DataExtractionAgentTools _dataTools;

    public SyncfusionDocumentAgentExtractor(NasSyncfusionDocumentStorage storage)
    {
        _storage = storage;
        _manager = new DocumentStorageManager(storage);
        _pdfTools = new PdfContentExtractionAgentTools(_manager);
        _wordTools = new WordImportExportAgentTools(_manager);
        _dataTools = new DataExtractionAgentTools(_manager);
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

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => ExtractPdf(workName, fileName),
            ".doc" or ".docx" => ExtractWord(workName),
            _ => new AgentExtractionResult(
                $"Syncfusion AgentTools: unsupported type {ext}. Upload PDF, Word, or plain text.",
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
        if (result.Success)
            return result.Message ?? result.Data?.ToString() ?? string.Empty;

        return result.Error ?? string.Empty;
    }

    private static int UnwrapInt(AgentToolResult result, int fallback)
    {
        if (!result.Success)
            return fallback;

        if (result.Data is int i)
            return i;

        if (result.Data is long l)
            return (int)l;

        if (int.TryParse(result.Message, out var fromMessage))
            return fromMessage;

        if (result.Data != null && int.TryParse(result.Data.ToString(), out var fromData))
            return fromData;

        return fallback;
    }
}
