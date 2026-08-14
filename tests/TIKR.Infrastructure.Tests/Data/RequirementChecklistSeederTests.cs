using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure;
using TIKR.Infrastructure.Tests.Helpers;

namespace TIKR.Infrastructure.Tests.Data;

public class RequirementChecklistSeederTests
{
    [Fact]
    public async Task SeedAsync_AddsElectionPlaybooks_AndIsIdempotent()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        await DbSeeder.SeedAsync(db);

        await RequirementChecklistSeeder.SeedAsync(db);
        await RequirementChecklistSeeder.SeedAsync(db);

        var canvass = await db.Requirements.SingleAsync(r => r.Title == "Election Canvass & Certification");
        var finance = await db.Requirements.SingleAsync(r => r.Title == "Campaign Finance Filing (Local)");
        var board = await db.Requirements.SingleAsync(r => r.Title == "Board Organizational Meeting");

        var canvassItems = await db.RequirementChecklistItems
            .Where(i => i.RequirementId == canvass.Id)
            .OrderBy(i => i.SortOrder)
            .ToListAsync();
        canvassItems.Should().HaveCount(5);
        canvassItems.Select(i => i.Title).Should().Contain("Assemble canvass packet");
        canvassItems.Select(i => i.Title).Should().Contain("File certification with county / SOS");

        (await db.RequirementChecklistItems.CountAsync(i => i.RequirementId == finance.Id)).Should().Be(4);
        (await db.RequirementChecklistItems.CountAsync(i => i.RequirementId == board.Id)).Should().Be(5);
    }
}
