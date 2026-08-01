using TIKR.Shared.DTOs;

namespace TIKR.Shared.Helpers;

/// <summary>
/// Colorado DOLG order of business for municipal meetings.
/// Reference: https://dlg.colorado.gov/Parliamentary-Procedure
/// </summary>
public static class CouncilAgendaScaffold
{
    public static string BoardDisplayName(string board) =>
        board.Equals("WSD", StringComparison.OrdinalIgnoreCase)
            ? "Wiley Sanitation District"
            : "Town of Wiley Board of Trustees";

    public static IReadOnlyList<CouncilAgendaSection> CreateOrderOfBusiness(
        string board,
        DateOnly meetingDate,
        DateOnly priorMeetingDate,
        IReadOnlyList<CouncilAgendaItem>? newBusinessItems = null)
    {
        var priorLabel = priorMeetingDate.ToString("MMMM d, yyyy");
        var locationNote = board.Equals("WSD", StringComparison.OrdinalIgnoreCase)
            ? "See posted notice for meeting location."
            : "Town Hall, 304 Main Street, Wiley CO — 6:00 PM";

        return
        [
            new CouncilAgendaSection(
                "call_to_order",
                "Call to Order",
                [],
                $"Call meeting to order. {locationNote}"),

            new CouncilAgendaSection(
                "approval_of_minutes",
                "Approval of Minutes",
                [
                    new CouncilAgendaItem(
                        $"Minutes of {priorLabel}",
                        "If there are no additions or corrections, minutes stand approved as printed.",
                        null)
                ]),

            new CouncilAgendaSection(
                "public_comment",
                "Public Comment",
                [],
                "Comments on matters not on the agenda per C.R.S. § 24-6-402."),

            new CouncilAgendaSection(
                "reports",
                "Reports",
                [],
                "Staff, committee, and officer reports."),

            new CouncilAgendaSection(
                "old_business",
                "Old Business / Unfinished Business",
                [],
                "Matters continued, tabled, or postponed from prior meetings."),

            new CouncilAgendaSection(
                "new_business",
                "New Business",
                newBusinessItems ?? [],
                null),

            new CouncilAgendaSection(
                "adjourn",
                "Adjourn",
                [],
                "If there is no further business, adjourn the meeting.")
        ];
    }

    public static CouncilAgendaSection? FindSection(
        IReadOnlyList<CouncilAgendaSection> sections,
        string sectionKey) =>
        sections.FirstOrDefault(s => s.SectionKey.Equals(sectionKey, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<CouncilAgendaSection> WithSectionItems(
        IReadOnlyList<CouncilAgendaSection> sections,
        string sectionKey,
        IReadOnlyList<CouncilAgendaItem> items)
    {
        return sections
            .Select(s => s.SectionKey.Equals(sectionKey, StringComparison.OrdinalIgnoreCase)
                ? s with { Items = items }
                : s)
            .ToList();
    }
}
