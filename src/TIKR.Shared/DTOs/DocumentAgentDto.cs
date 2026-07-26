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
    bool UsedSyncfusionTools,
    string? OriginalStoragePath = null,
    string? ProcessedStoragePath = null,
    string? StructuredTables = null,
    string? SuggestedSubmitTo = null,
    string? SuggestedContactName = null,
    string? SuggestedContactEmail = null,
    string? SuggestedContactPhone = null);
