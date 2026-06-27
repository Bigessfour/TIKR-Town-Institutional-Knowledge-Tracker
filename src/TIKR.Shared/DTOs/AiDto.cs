namespace TIKR.Shared.DTOs;

public record TagDocumentRequest(Guid DocumentId);

public record TagDocumentResponse(Guid DocumentId, string[] Tags, string? SuggestedFolder);

public record DashboardPriority(
    string Title,
    string Reason,
    DateOnly? DueDate,
    string Priority);

public record AskAdvancedRequest(string Prompt, string? Context);

public record AskAdvancedResponse(string Answer, bool UsedGrok);

public record AiStatusResponse(
    bool OllamaAvailable,
    string OllamaModel,
    bool GrokEnabled);
