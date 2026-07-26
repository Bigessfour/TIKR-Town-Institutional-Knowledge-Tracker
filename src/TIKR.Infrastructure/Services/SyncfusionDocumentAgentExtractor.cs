using System.Text.Json;
using Syncfusion.AI.AgentTools.Core;
using Syncfusion.AI.AgentTools.DataExtraction;
using Syncfusion.AI.AgentTools.OfficeToPDF;
using Syncfusion.AI.AgentTools.PDF;
using Syncfusion.AI.AgentTools.PowerPoint;
using Syncfusion.AI.AgentTools.Word;
using TIKR.Shared.Interfaces;
using TIKR.SyncfusionDocuments;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// Deterministic Syncfusion Storage Mode extraction for Requirements AI Scan (Phase 10C-A2).
/// Orchestrated tool selection via <see cref="SyncfusionDocumentAgentOrchestrator"/> when enabled (A3).
/// Sparse PDF/Word text is enriched with <see cref="IDocumentOcrService"/> (Tesseract OCR).
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
    private readonly IDocumentOcrService _ocr;

    public SyncfusionDocumentAgentExtractor(
        NasSyncfusionDocumentStorage storage,
        SyncfusionDocumentAgentOrchestrator orchestrator,
        IDocumentOcrService ocr)
    {
        _storage = storage;
        _orchestrator = orchestrator;
        _ocr = ocr;
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
        {
            // Orchestrator returns text; still fill StructuredTables for PDFs when empty.
            if (string.IsNullOrWhiteSpace(orchestrated.StructuredTables)
                && Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                var (tables, tableJson) = TryExtractTables(workName, fileName);
                if (!string.IsNullOrWhiteSpace(tableJson))
                {
                    return orchestrated with
                    {
                        TablesExtractedCount = tables > 0 ? tables : orchestrated.TablesExtractedCount,
                        StructuredTables = tableJson
                    };
                }
            }

            return orchestrated;
        }

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
        var usedOcr = false;
        if (_ocr.IsEnabled && SyncfusionDocumentOcrService.NeedsOcr(text))
        {
            using var pdfStream = _storage.Read(workName);
            var ocr = _ocr.EnrichPdf(pdfStream, text);
            if (!string.IsNullOrWhiteSpace(ocr.Text) &&
                (ocr.UsedOcr || SyncfusionDocumentOcrService.CountLetters(ocr.Text) > SyncfusionDocumentOcrService.CountLetters(text)))
            {
                text = ocr.Text;
                usedOcr = ocr.UsedOcr;
            }
        }

        var (tables, tableJson) = TryExtractTables(workName, originalFileName);
        return new AgentExtractionResult(text, tables, UsedSyncfusionTools: true, tableJson, usedOcr);
    }

    private AgentExtractionResult ExtractWord(string workName)
    {
        var text = UnwrapPayload(_wordTools.GetText(workName));
        var usedOcr = false;
        if (_ocr.IsEnabled && SyncfusionDocumentOcrService.NeedsOcr(text))
        {
            using var wordStream = _storage.Read(workName);
            var ocr = _ocr.EnrichWord(wordStream, workName, text);
            if (!string.IsNullOrWhiteSpace(ocr.Text) &&
                (ocr.UsedOcr || SyncfusionDocumentOcrService.CountLetters(ocr.Text) > SyncfusionDocumentOcrService.CountLetters(text)))
            {
                text = ocr.Text;
                usedOcr = ocr.UsedOcr;
            }
        }

        return new AgentExtractionResult(text, 1, UsedSyncfusionTools: true, UsedOcr: usedOcr);
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

    private (int Count, string? Json) TryExtractTables(string workName, string originalFileName)
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
                return (DocumentAgentService.InferTableCount(originalFileName), null);

            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                return (doc.RootElement.GetArrayLength(), json);

            if (doc.RootElement.TryGetProperty("tables", out var tables) && tables.ValueKind == JsonValueKind.Array)
                return (tables.GetArrayLength(), json);
        }
        catch
        {
            // Best-effort — clerk still gets text.
        }

        return (DocumentAgentService.InferTableCount(originalFileName), null);
    }

    private int TryCountTables(string workName, string originalFileName) =>
        TryExtractTables(workName, originalFileName).Count;

    /// <summary>
    /// Pulls clerk-usable text from Syncfusion <see cref="AgentToolResult"/> payloads.
    /// Prefer property reflection over <c>Data.ToString()</c>, which often dumps
    /// <c>{ DocumentId = …, Text = …, PageCount = N }</c> instead of plain page text.
    /// </summary>
    internal static string UnwrapPayload(AgentToolResult result)
    {
        if (!result.Success)
            return result.Error ?? string.Empty;

        var fromData = ExtractTextFromData(result.Data);
        if (!string.IsNullOrWhiteSpace(fromData) &&
            !fromData.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return fromData;

        return NormalizeExtractedText(result.Message) ?? fromData ?? string.Empty;
    }

    internal static string? ExtractTextFromData(object? data)
    {
        if (data is null)
            return null;

        if (data is string s)
            return NormalizeExtractedText(s);

        // Known Syncfusion payload shapes expose Text / Content / ExtractedText.
        foreach (var name in new[] { "Text", "Content", "ExtractedText", "Value" })
        {
            var prop = data.GetType().GetProperty(name);
            if (prop is null || prop.PropertyType != typeof(string))
                continue;
            if (prop.GetValue(data) is string value && !string.IsNullOrWhiteSpace(value))
                return NormalizeExtractedText(value);
        }

        return NormalizeExtractedText(data.ToString());
    }

    /// <summary>
    /// Strips Syncfusion agent-tool object dumps so RAG stores plain passage text.
    /// Example dump: <c>{ DocumentId = x.pdf, Text = --- Page 1 --- Form W-9 …, PageCount = 1 }</c>
    /// </summary>
    internal static string? NormalizeExtractedText(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return raw;

        var trimmed = raw.Trim();

        // Full object dump with PageCount terminator (multiline Text allowed).
        var withPageCount = System.Text.RegularExpressions.Regex.Match(
            trimmed,
            @"^\{\s*DocumentId\s*=\s*.*?,\s*Text\s*=\s*(.*)\s*,\s*PageCount\s*=\s*\d+\s*\}\s*$",
            System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (withPageCount.Success)
            return withPageCount.Groups[1].Value.Trim();

        // Simpler { Text = ... } dump (Word tools).
        var textOnly = System.Text.RegularExpressions.Regex.Match(
            trimmed,
            @"^\{\s*Text\s*=\s*(.*?)\s*\}\s*$",
            System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        if (textOnly.Success)
            return textOnly.Groups[1].Value.Trim();

        // Loose: leading "{ DocumentId = …, Text = " without requiring trailing structure.
        const string marker = "Text = ";
        var idx = trimmed.IndexOf(marker, StringComparison.Ordinal);
        if (trimmed.StartsWith('{') && idx > 0)
        {
            var start = idx + marker.Length;
            var end = trimmed.LastIndexOf(", PageCount", StringComparison.Ordinal);
            if (end > start)
                return trimmed[start..end].Trim();
            if (trimmed.EndsWith('}'))
                return trimmed[start..^1].Trim();
        }

        return trimmed;
    }
}

