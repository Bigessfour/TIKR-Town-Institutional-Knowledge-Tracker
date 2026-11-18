namespace TIKR.Shared.DTOs;

/// <summary>Snapshot of Ollama host reachability and required model presence.</summary>
public record OllamaModelInventoryDto(
    bool HostReachable,
    bool ChatModelPresent,
    bool EmbeddingModelPresent,
    int LoadedModelCount,
    string StatusMessage);
