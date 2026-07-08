using System.Text;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
using TIKR.Shared.Helpers;

namespace TIKR.Web.Helpers;

public static class RequirementWorkflowHelpers
{
    public static RequirementUrgency GetUrgency(RequirementDto requirement, DateOnly? referenceDate = null) =>
        RequirementUrgencyHelper.GetUrgency(requirement, referenceDate);

    public static string GetUrgencyLabel(RequirementUrgency urgency) =>
        RequirementUrgencyHelper.GetLabel(urgency);

    public static string GetUrgencyCssClass(RequirementUrgency urgency) => urgency switch
    {
        RequirementUrgency.Overdue => "priority-overdue",
        RequirementUrgency.High => "priority-high",
        RequirementUrgency.Medium => "priority-medium",
        RequirementUrgency.Low => "priority-low",
        RequirementUrgency.Completed => "urgency-completed",
        _ => ""
    };

    public static IEnumerable<RequirementDto> FilterRequirements(
        IEnumerable<RequirementDto> source,
        string? searchText,
        RequirementCategory? categoryFilter,
        RequirementUrgency? urgencyFilter,
        bool includeCompleted,
        DateOnly? referenceDate = null)
    {
        var today = referenceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        IEnumerable<RequirementDto> query = source;

        if (!includeCompleted)
            query = query.Where(r => !r.IsCompleted);

        if (categoryFilter.HasValue)
            query = query.Where(r => r.Category == categoryFilter.Value);

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var term = searchText.Trim();
            query = query.Where(r =>
                r.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (r.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (urgencyFilter.HasValue)
            query = query.Where(r => GetUrgency(r, today) == urgencyFilter.Value);

        return query.OrderBy(r => r.DueDate);
    }

    public static string BuildCsv(IEnumerable<RequirementDto> requirements, DateOnly? referenceDate = null)
    {
        var today = referenceDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var sb = new StringBuilder();
        sb.AppendLine("Title,Description,Due Date,Recurrence,Category,Urgency,CO Default,Completed");

        foreach (var requirement in requirements.OrderBy(r => r.DueDate))
        {
            var urgency = GetUrgencyLabel(GetUrgency(requirement, today));
            sb.Append(CsvEscape(requirement.Title));
            sb.Append(',');
            sb.Append(CsvEscape(requirement.Description ?? string.Empty));
            sb.Append(',');
            sb.Append(requirement.DueDate.ToString("yyyy-MM-dd"));
            sb.Append(',');
            sb.Append(requirement.Recurrence);
            sb.Append(',');
            sb.Append(requirement.Category);
            sb.Append(',');
            sb.Append(urgency);
            sb.Append(',');
            sb.Append(requirement.IsSystemSeeded ? "Yes" : "No");
            sb.Append(',');
            sb.AppendLine(requirement.IsCompleted ? "Yes" : "No");
        }

        return sb.ToString();
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        return value;
    }

    public static CreateRequirementRequest ApplyAgentExtraction(DocumentAgentResult result)
    {
        var dueDate = result.SuggestedDueDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1));

        // Enhanced for archive extension: incorporate structured tables into Description when available
        // (tiny feature: better form field population from agent tables)
        var description = result.ExtractedText;
        if (!string.IsNullOrWhiteSpace(result.StructuredTables) && result.StructuredTables != result.ExtractedText)
        {
            description = (result.ExtractedText ?? string.Empty) +
                          "\n\n[Structured tables from document]\n" + result.StructuredTables;
        }

        return new CreateRequirementRequest(
            result.SuggestedTitle,
            description,
            dueDate,
            result.SuggestedRecurrence,
            result.SuggestedCategory);
    }

    public static string FormatAgentScanMessage(DocumentAgentResult result)
    {
        var source = result.UsedSyncfusionTools ? "Syncfusion Document SDK" : "Plain-text extraction";
        return $"Processed on Synology • {source} • {result.TablesExtractedCount} table{(result.TablesExtractedCount == 1 ? "" : "s")} extracted";
    }

    public static string GetExtractionBadgeCssClass(DocumentAgentResult result) =>
        result.UsedSyncfusionTools ? "extraction-badge-syncfusion" : "extraction-badge-stub";

    public static string GetExtractionSourceLabel(DocumentAgentResult result) =>
        result.UsedSyncfusionTools ? "Syncfusion Document SDK" : "Plain-text extraction";

    public static CreateRequirementRequest ToCreateRequest(RequirementDto requirement) =>
        new(requirement.Title, requirement.Description, requirement.DueDate, requirement.Recurrence, requirement.Category);
}
