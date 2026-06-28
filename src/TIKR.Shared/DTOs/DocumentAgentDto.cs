using TIKR.Shared.Enums;

namespace TIKR.Shared.DTOs;

public record DocumentAgentResult(
    string SuggestedTitle,
    string? ExtractedText,
    DateOnly? SuggestedDueDate,
    RecurrenceType SuggestedRecurrence,
    RequirementCategory SuggestedCategory,
    int TablesExtractedCount,
    string StoragePath,
    bool ProcessedLocally,
    bool UsedSyncfusionTools);
