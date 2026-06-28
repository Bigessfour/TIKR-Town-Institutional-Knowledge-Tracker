using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure;
using TIKR.Infrastructure.Tests.Helpers;

namespace TIKR.Infrastructure.Tests.Data;

public class DbSeederTests
{
    [Fact]
    public async Task SeedAsync_InsertsFifteenColoradoDeadlines()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();

        await DbSeeder.SeedAsync(db);

        var seeded = await db.Requirements.Where(r => r.IsSystemSeeded).ToListAsync();
        seeded.Should().HaveCount(15);
        seeded.Should().OnlyContain(r => r.IsSystemSeeded);
        seeded.Select(r => r.DueDate.Year).Should().AllBeEquivalentTo(DateTime.UtcNow.Year);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();

        await DbSeeder.SeedAsync(db);
        await DbSeeder.SeedAsync(db);

        (await db.Requirements.CountAsync()).Should().Be(15);
    }

    [Fact]
    public async Task SeedAsync_IncludesKnownColoradoDeadlines()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();

        await DbSeeder.SeedAsync(db);

        var titles = await db.Requirements.Where(r => r.IsSystemSeeded).Select(r => r.Title).ToListAsync();
        titles.Should().Contain("Annual Budget Submission");
        titles.Should().Contain("Mill Levy Certification");
        titles.Should().Contain("TABOR Revenue Report");

        var millLevy = await db.Requirements.SingleAsync(r => r.Title == "Mill Levy Certification");
        millLevy.Category.Should().Be(Shared.Enums.RequirementCategory.MillLevy);
        millLevy.DueDate.Month.Should().Be(12);
        millLevy.DueDate.Day.Should().Be(15);
    }
}
