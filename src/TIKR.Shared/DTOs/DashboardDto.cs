namespace TIKR.Shared.DTOs;

public record DashboardDueOutDto(
    Guid RequirementId,
    string Title,
    string? Description,
    DateOnly DueDate,
    string Urgency,
    string? SubmitTo,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    bool IsCompleted,
    int LinkedDocumentCount,
    IReadOnlyList<RequirementLinkedDocumentDto> LinkedDocuments);

public record DashboardSummaryDto(
    int OverdueCount,
    int HighCount,
    int MediumCount,
    int LowCount,
    int MissingPacketCount,
    IReadOnlyList<DashboardDueOutDto> DueOuts);

public record DashboardLayoutPanelDto(
    string PanelId,
    int Column,
    int Row,
    int SizeX,
    int SizeY,
    int MinSizeX = 1,
    int MinSizeY = 1);
