using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// NAS-local document agent orchestration. Extraction backend is swappable (stub vs Syncfusion AgentTools).
/// </summary>
public class DocumentAgentService(
    IAgentDocumentStorage agentStorage,
    IDocumentAgentExtractionBackend extractionBackend) : IDocumentAgentService
{
    public async Task<DocumentAgentResult> ProcessUploadAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        var storagePath = await agentStorage.SaveAgentScanAsync(new MemoryStream(bytes), fileName, cancellationToken);

        await using var extractStream = new MemoryStream(bytes);
        var extraction = await extractionBackend.ExtractAsync(extractStream, fileName, cancellationToken);

        var title = DeriveTitle(fileName);
        var category = InferCategory(title);

        return new DocumentAgentResult(
            SuggestedTitle: title,
            ExtractedText: extraction.ExtractedText,
            SuggestedDueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            SuggestedRecurrence: RecurrenceType.Annual,
            SuggestedCategory: category,
            TablesExtractedCount: extraction.TablesExtractedCount,
            StoragePath: storagePath,
            ProcessedLocally: true,
            UsedSyncfusionTools: extraction.UsedSyncfusionTools);
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
            ".xlsx" or ".csv" => 2,
            _ => 1
        };
    }
}
