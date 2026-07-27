using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TIKR.Shared.Diagnostics;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
using TIKR.Shared.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// NAS-local document agent orchestration. Extraction backend is swappable (stub vs Syncfusion AgentTools).
/// Extended for Grok Heavy recommended feature: dual original + stamped PDF archive storage.
/// </summary>
public class DocumentAgentService(
    IAgentDocumentStorage agentStorage,
    IDocumentAgentExtractionBackend extractionBackend,
    IDocumentGenerationService? documentGenerationService = null,
    ILogger<DocumentAgentService>? logger = null) : IDocumentAgentService
{
    private readonly ILogger _log = logger ?? NullLogger<DocumentAgentService>.Instance;

    public async Task<DocumentAgentResult> ProcessUploadAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        TikrActionLog.Started(_log, "Agent.Scan", $"FileName={fileName}");

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        TikrActionLog.Info(_log, "Agent.Scan", $"Buffered {bytes.Length} bytes for {fileName}");

        // 1. Always save the original (existing behavior)
        var originalPath = await agentStorage.SaveAgentScanAsync(new MemoryStream(bytes), fileName, cancellationToken);
        TikrActionLog.Info(_log, "Agent.Scan", $"Saved original StoragePath={originalPath}");

        // 2. Extract text/tables (existing)
        await using var extractStream = new MemoryStream(bytes);
        var extraction = await extractionBackend.ExtractAsync(extractStream, fileName, cancellationToken);
        TikrActionLog.Info(_log, "Agent.Scan",
            $"Extracted TextChars={extraction.ExtractedText?.Length ?? 0} Tables={extraction.TablesExtractedCount} UsedSyncfusion={extraction.UsedSyncfusionTools}");

        var title = DeriveTitle(fileName);
        var category = InferCategory(title);
        var parsed = DueOutFieldParser.Parse(
            string.Join("\n", new[] { extraction.ExtractedText, extraction.StructuredTables }.Where(s => !string.IsNullOrWhiteSpace(s))));

        string? processedPath = null;
        var structuredTables = extraction.StructuredTables;

        // 3. Grok Heavy recommended: create clean stamped PDF archive copy + dual storage (original + processed)
        // when Syncfusion tools were used for extraction. The archive generator always returns a
        // .ai-archive.pdf named result; storage layer scopes under agent-scans/.
        if (extraction.UsedSyncfusionTools && documentGenerationService is not null)
        {
            try
            {
                await using var archiveInput = new MemoryStream(bytes);
                var archiveResult = await documentGenerationService.CreateAgentArchivePdfAsync(
                    archiveInput, fileName, DateTime.UtcNow, cancellationToken);

                await using var processedStream = new MemoryStream(archiveResult.Content);
                var processedFileName = archiveResult.FileName ?? Path.ChangeExtension(fileName, ".ai-archive.pdf");
                // Pass clean filename (no dir prefix). NasAgentDocumentStorage + LocalFileStorageService
                // ensure agent-scans/ scoping; filename suffix distinguishes processed copy.
                processedPath = await agentStorage.SaveAgentScanAsync(processedStream, processedFileName, cancellationToken);
            }
            catch (Exception ex)
            {
                // Best effort: continue with original only on generation/storage edge cases
                TikrActionLog.Failed(_log, "Agent.Scan.Archive", ex, $"FileName={fileName}");
            }
        }

        // 4. Return enhanced result with dual paths (original kept for fidelity; processed preferred for display/use)
        TikrActionLog.Completed(_log, "Agent.Scan",
            $"Title={title} Category={category} Due={parsed.DueDate} SubmitTo={parsed.SubmitTo ?? "(none)"} ProcessedPath={processedPath ?? "(none)"}",
            sw.ElapsedMilliseconds);

        return new DocumentAgentResult(
            SuggestedTitle: title,
            ExtractedText: extraction.ExtractedText,
            SuggestedDueDate: parsed.DueDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            SuggestedRecurrence: RecurrenceType.Annual,
            SuggestedCategory: category,
            TablesExtractedCount: extraction.TablesExtractedCount,
            StoragePath: processedPath ?? originalPath,
            ProcessedLocally: true,
            UsedSyncfusionTools: extraction.UsedSyncfusionTools,
            OriginalStoragePath: originalPath,
            ProcessedStoragePath: processedPath,
            StructuredTables: structuredTables,
            SuggestedSubmitTo: parsed.SubmitTo,
            SuggestedContactName: parsed.ContactName,
            SuggestedContactEmail: parsed.ContactEmail,
            SuggestedContactPhone: parsed.ContactPhone);
    }

    internal static string DeriveTitle(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName).Replace('_', ' ').Replace('-', ' ').Trim();
        return string.IsNullOrWhiteSpace(name) ? "Imported requirement" : name;
    }

    internal static RequirementCategory InferCategory(string title)
    {
        var lower = title.ToLowerInvariant();
        if (lower.Contains("budget", StringComparison.Ordinal)) return RequirementCategory.Budget;
        if (lower.Contains("audit", StringComparison.Ordinal)) return RequirementCategory.Audit;
        if (lower.Contains("election", StringComparison.Ordinal) || lower.Contains("canvass", StringComparison.Ordinal))
            return RequirementCategory.Election;
        if (lower.Contains("mill", StringComparison.Ordinal) || lower.Contains("levy", StringComparison.Ordinal))
            return RequirementCategory.MillLevy;
        if (lower.Contains("tabor", StringComparison.Ordinal) || lower.Contains("compliance", StringComparison.Ordinal))
            return RequirementCategory.Compliance;
        return RequirementCategory.Custom;
    }

    internal static int InferTableCount(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".pdf" => 3,
            ".xlsx" or ".xls" or ".csv" => 2,
            ".ppt" or ".pptx" => 1,
            _ => 1
        };
    }
}
