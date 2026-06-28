using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// In-memory MVP stub for document agent extraction. Syncfusion DocumentSDK AgentTools
/// integration is Phase 10 group A — this proves NAS-local save + requirement mapping.
/// </summary>
public class DocumentAgentService(IFileStorageService storage) : IDocumentAgentService
{
    public async Task<DocumentAgentResult> ProcessUploadAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var storagePath = await SynologyDocumentStorage.SaveAgentScanAsync(storage, content, fileName, cancellationToken);
        var title = DeriveTitle(fileName);
        var category = InferCategory(title);
        var tables = InferTableCount(fileName);

        return new DocumentAgentResult(
            SuggestedTitle: title,
            ExtractedText: $"Agent stub extracted text from {Path.GetFileName(fileName)} (saved to NAS volume).",
            SuggestedDueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1)),
            SuggestedRecurrence: RecurrenceType.Annual,
            SuggestedCategory: category,
            TablesExtractedCount: tables,
            StoragePath: storagePath,
            ProcessedLocally: true);
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
