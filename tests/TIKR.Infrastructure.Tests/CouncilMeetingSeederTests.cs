using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure;
using TIKR.Infrastructure.Tests.Helpers;
using TIKR.Shared.Helpers;

namespace TIKR.Infrastructure.Tests;

public class CouncilMeetingSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesThirtyRequirementsForAugDecTowAndWsd()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        await CouncilMeetingSeeder.SeedAsync(db);

        var rows = await db.Requirements
            .Where(r => r.Description != null && r.Description.Contains(CouncilMeetingSeeder.CycleMarker))
            .ToListAsync();

        rows.Should().HaveCount(30);
        rows.Should().OnlyContain(r => r.IsSystemSeeded);
        rows.Count(r => r.Title.Contains("Town Council", StringComparison.Ordinal)).Should().Be(15);
        rows.Count(r => r.Title.StartsWith("Post WSD", StringComparison.Ordinal) ||
                        r.Title.StartsWith("Draft WSD", StringComparison.Ordinal) ||
                        r.Title.StartsWith("WSD Regular", StringComparison.Ordinal)).Should().Be(15);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        await CouncilMeetingSeeder.SeedAsync(db);
        await CouncilMeetingSeeder.SeedAsync(db);

        var count = await db.Requirements.CountAsync(r =>
            r.Description != null &&
            r.Description.Contains(CouncilMeetingSeeder.CycleMarker));

        count.Should().Be(30);
    }

    [Fact]
    public async Task SeedAsync_AgendaDueTwoDaysBeforeMeeting()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        await CouncilMeetingSeeder.SeedAsync(db);

        var meeting = await db.Requirements.SingleAsync(r =>
            r.Title == "Town Council Regular Meeting — August 10, 2026");

        var agenda = await db.Requirements.SingleAsync(r =>
            r.Title == "Post Town Council Agenda — August 10, 2026");

        agenda.DueDate.Should().Be(meeting.DueDate.AddDays(-CouncilMeetingSchedule.DefaultAgendaLeadDays));
    }

    [Fact]
    public async Task SeedAsync_MinutesDueTwoDaysAfterMeeting()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        await CouncilMeetingSeeder.SeedAsync(db);

        var meeting = await db.Requirements.SingleAsync(r =>
            r.Title == "WSD Regular Meeting — December 14, 2026");

        var minutes = await db.Requirements.SingleAsync(r =>
            r.Title == "Draft WSD Minutes — December 14, 2026");

        minutes.DueDate.Should().Be(meeting.DueDate.AddDays(CouncilMeetingSchedule.DefaultMinutesLagDays));
    }
}
