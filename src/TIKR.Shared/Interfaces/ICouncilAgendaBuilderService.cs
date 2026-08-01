using TIKR.Shared.DTOs;

namespace TIKR.Shared.Interfaces;

public interface ICouncilAgendaBuilderService
{
    Task<CouncilAgendaBuilderPreview> BuildPreviewAsync(
        DateOnly meetingDate,
        string board = "TOW",
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UnfinishedBusinessSuggestion>> SuggestUnfinishedBusinessAsync(
        DateOnly meetingDate,
        string board = "TOW",
        CancellationToken cancellationToken = default);

    Task<CouncilMinutesBuilderPreview> BuildMinutesPreviewAsync(
        DateOnly meetingDate,
        string board = "TOW",
        CancellationToken cancellationToken = default);
}
