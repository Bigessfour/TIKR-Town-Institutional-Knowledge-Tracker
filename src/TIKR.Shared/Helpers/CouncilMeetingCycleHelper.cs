namespace TIKR.Shared.Helpers;

/// <summary>Parses seeded council meeting cycle markers on <see cref="Entities.Requirement"/> descriptions.</summary>
public static class CouncilMeetingCycleHelper
{
    public static bool TryParse(
        string? description,
        out string? kind,
        out string? board,
        out DateOnly? meetingDate)
    {
        kind = null;
        board = null;
        meetingDate = null;

        if (string.IsNullOrWhiteSpace(description)
            || !description.Contains(CouncilMeetingCycle.Marker, StringComparison.Ordinal))
            return false;

        foreach (var segment in description.Split(';', StringSplitOptions.TrimEntries))
        {
            if (segment.StartsWith("kind=", StringComparison.OrdinalIgnoreCase))
                kind = segment["kind=".Length..].Trim();
            else if (segment.StartsWith("board=", StringComparison.OrdinalIgnoreCase))
                board = segment["board=".Length..].Trim();
            else if (segment.StartsWith("meeting=", StringComparison.OrdinalIgnoreCase))
            {
                var raw = segment["meeting=".Length..].Trim();
                var dateToken = raw.Split([' ', '.'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .FirstOrDefault();
                if (dateToken is not null && DateOnly.TryParse(dateToken, out var parsed))
                    meetingDate = parsed;
            }
        }

        return meetingDate is not null
               && !string.IsNullOrWhiteSpace(kind)
               && !string.IsNullOrWhiteSpace(board);
    }

    public static bool Matches(string? description, string board, DateOnly meetingDate, string kind) =>
        TryParse(description, out var parsedKind, out var parsedBoard, out var parsedDate)
        && parsedKind!.Equals(kind, StringComparison.OrdinalIgnoreCase)
        && parsedBoard!.Equals(board, StringComparison.OrdinalIgnoreCase)
        && parsedDate == meetingDate;
}
