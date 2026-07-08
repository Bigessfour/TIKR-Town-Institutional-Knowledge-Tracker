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
    // New for Grok Heavy recommended archive extension (10C-G):
    // dual storage of original + clean stamped PDF archive copy
    string? OriginalStoragePath = null,
    string? ProcessedStoragePath = null,
    // Structured table data (JSON) for mapping into Requirement form fields where possible
    string? StructuredTables = null);
