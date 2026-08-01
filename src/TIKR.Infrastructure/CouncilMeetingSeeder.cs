using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;
using TIKR.Shared.Helpers;

namespace TIKR.Infrastructure;

/// <summary>
/// Seeds Town Council (TOW) and WSD meeting-cycle requirements for Aug–Dec 2026.
/// Idempotent via <see cref="CycleMarker"/> in requirement descriptions.
/// </summary>
public static class CouncilMeetingSeeder
{
    public const string CycleMarker = CouncilMeetingCycle.Marker;

    public const int SeedYear = 2026;
    public const int SeedFirstMonth = 8;
    public const int SeedLastMonth = 12;

    public static async Task SeedAsync(TikrDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Requirements.AnyAsync(
                r => r.Description != null && r.Description.Contains(CycleMarker),
                cancellationToken))
            return;

        var requirements = new List<Requirement>();
        foreach (var meetingDate in CouncilMeetingSchedule.SecondMondaysInRange(
                     SeedYear, SeedFirstMonth, SeedLastMonth))
        {
            requirements.AddRange(CreateBoardCycle(meetingDate, CouncilBoard.Tow));
            requirements.AddRange(CreateBoardCycle(meetingDate, CouncilBoard.Wsd));
        }

        db.Requirements.AddRange(requirements);
        await db.SaveChangesAsync(cancellationToken);
    }

    internal enum CouncilBoard
    {
        Tow,
        Wsd
    }

    internal static IEnumerable<Requirement> CreateBoardCycle(DateOnly meetingDate, CouncilBoard board)
    {
        var (boardLabel, submitTo, locationNote) = board switch
        {
            CouncilBoard.Tow => (
                "Town Council",
                "Board of Trustees",
                "Town Hall, 304 Main Street, Wiley CO — 6:00 PM"),
            CouncilBoard.Wsd => (
                "WSD",
                "Wiley Sanitation District",
                "Regular district meeting — 6:00 PM (see posted agenda for location)"),
            _ => throw new ArgumentOutOfRangeException(nameof(board))
        };

        var meetingKey = meetingDate.ToString("yyyy-MM-dd");
        var displayDate = meetingDate.ToString("MMMM d, yyyy");
        var boardToken = board.ToString().ToUpperInvariant();

        yield return new Requirement
        {
            Id = Guid.NewGuid(),
            Title = $"{boardLabel} Regular Meeting — {displayDate}",
            Description =
                $"{CycleMarker}; kind=Meeting; board={boardToken}; meeting={meetingKey}. {locationNote}.",
            DueDate = meetingDate,
            Recurrence = RecurrenceType.None,
            Category = RequirementCategory.Compliance,
            IsSystemSeeded = true,
            SubmitTo = submitTo
        };

        yield return new Requirement
        {
            Id = Guid.NewGuid(),
            Title = $"Post {boardLabel} Agenda — {displayDate}",
            Description =
                $"{CycleMarker}; kind=PostAgenda; board={boardToken}; meeting={meetingKey}. " +
                $"Post/build agenda at least {CouncilMeetingSchedule.DefaultAgendaLeadDays} days before the meeting (C.R.S. § 24-6-402).",
            DueDate = meetingDate.AddDays(-CouncilMeetingSchedule.DefaultAgendaLeadDays),
            Recurrence = RecurrenceType.None,
            Category = RequirementCategory.Compliance,
            IsSystemSeeded = true,
            SubmitTo = "Public notice / town website"
        };

        yield return new Requirement
        {
            Id = Guid.NewGuid(),
            Title = $"Draft {boardLabel} Minutes — {displayDate}",
            Description =
                $"{CycleMarker}; kind=DraftMinutes; board={boardToken}; meeting={meetingKey}. " +
                "Draft minutes from the actioned agenda to close the meeting cycle.",
            DueDate = meetingDate.AddDays(CouncilMeetingSchedule.DefaultMinutesLagDays),
            Recurrence = RecurrenceType.None,
            Category = RequirementCategory.Compliance,
            IsSystemSeeded = true,
            SubmitTo = "Town records"
        };
    }
}
