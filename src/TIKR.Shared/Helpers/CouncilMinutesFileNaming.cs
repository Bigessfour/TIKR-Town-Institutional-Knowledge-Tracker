namespace TIKR.Shared.Helpers;

/// <summary>NAS-style council minutes filenames (e.g. 8 AUGUST 10 2026 TOW.docx).</summary>
public static class CouncilMinutesFileNaming
{
    public static string SuggestFileName(DateOnly meetingDate, string board)
    {
        var token = board.Equals("WSD", StringComparison.OrdinalIgnoreCase) ? "WSD" : "TOW";
        return $"{meetingDate.Month} {meetingDate:MMMM} {meetingDate.Day} {meetingDate.Year} {token}.docx"
            .ToUpperInvariant();
    }

    public static IReadOnlyList<string> FlattenAgendaSections(IReadOnlyList<DTOs.CouncilAgendaSection> sections)
    {
        var lines = new List<string>();
        foreach (var section in sections)
        {
            if (section.Items.Count == 0)
            {
                if (!IsProceduralOnly(section.SectionKey))
                    lines.Add(section.Title);
                continue;
            }

            foreach (var item in section.Items)
            {
                var line = string.IsNullOrWhiteSpace(item.Description)
                    ? item.Title
                    : $"{item.Title} — {item.Description}";
                lines.Add(line);
            }
        }

        return lines;
    }

    private static bool IsProceduralOnly(string sectionKey) =>
        sectionKey is "adjourn";
}
