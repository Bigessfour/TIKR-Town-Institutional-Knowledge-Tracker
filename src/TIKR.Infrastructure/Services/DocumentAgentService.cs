using Microsoft.Extensions.Logging;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
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
    public async Task<DocumentAgentResult> ProcessUploadAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        // 1. Always save the original (existing behavior)
        var originalPath = await agentStorage.SaveAgentScanAsync(new MemoryStream(bytes), fileName, cancellationToken);

        // 2. Extract text/tables (existing)
        await using var extractStream = new MemoryStream(bytes);
        var extraction = await extractionBackend.ExtractAsync(extractStream, fileName, cancellationToken);

        var title = DeriveTitle(fileName);
        var category = InferCategory(title);

        string? processedPath = null;
        string? structuredTables = extraction.ExtractedText; // basic; enhanced below if tables available

        // 3. Grok Heavy recommended: create clean tagged PDF archive copy + dual storage (when Syncfusion available)
        if (extraction.UsedSyncfusionTools && documentGenerationService is not null)
        {
            try
            {
                await using var archiveInput = new MemoryStream(bytes);
                var archiveResult = await documentGenerationService.CreateAgentArchivePdfAsync(
                    archiveInput, fileName, DateTime.UtcNow, cancellationToken);

                // Save the processed archive version under a distinct processed path
                await using var processedStream = new MemoryStream(archiveResult.Content);
                var processedFileName = archiveResult.FileName ?? Path.ChangeExtension(fileName, ".ai-archive.pdf");
                processedPath = await agentStorage.SaveAgentScanAsync(processedStream, $"processed/{processedFileName}", cancellationToken);

                // For structured tables, prefer richer data if the generation or future extraction provides it.
                // Currently we surface the extracted text; future enhancement can parse JSON tables here.
            }
            catch (Exception ex)
            {
                // Best effort: if archive creation fails (e.g. license edge), continue with original only
                logger?.LogWarning(ex, "Agent archive PDF creation failed for {File}", fileName);
            }
        }

        // 4. Return enhanced result with dual paths and structured hint
        return new DocumentAgentResult(
            SuggestedTitle: title,
            ExtractedText: extraction.ExtractedText,
            SuggestedDueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            SuggestedRecurrence: RecurrenceType.Annual,
            SuggestedCategory: category,
            TablesExtractedCount: extraction.TablesExtractedCount,
            StoragePath: processedPath ?? originalPath,   // prefer processed as primary StoragePath for archive use
            ProcessedLocally: true,
            UsedSyncfusionTools: extraction.UsedSyncfusionTools,
            OriginalStoragePath: originalPath,
            ProcessedStoragePath: processedPath,
            StructuredTables: structuredTables);
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
