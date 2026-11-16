namespace TIKR.Shared.DTOs;

public record LocalStorageStatusDto(
    string TownName,
    string StorageLabel,
    DateTime? DataLastModifiedUtc,
    bool OllamaAvailable,
    bool ModelMismatch = false,
    string? RunningModel = null);
