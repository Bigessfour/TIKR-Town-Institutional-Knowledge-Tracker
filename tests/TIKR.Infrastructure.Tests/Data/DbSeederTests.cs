using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure;
using TIKR.Infrastructure.Tests.Helpers;

namespace TIKR.Infrastructure.Tests.Data;

public class DbSeederTests
{
    [Fact]
    public async Task SeedAsync_InsertsSevenColoradoDeadlines()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();

        await DbSeeder.SeedAsync(db);

        var seeded = await db.Requirements.Where(r => r.IsSystemSeeded).ToListAsync();
        seeded.Should().HaveCount(7);
        seeded.Should().OnlyContain(r => r.IsSystemSeeded);
        seeded.Select(r => r.DueDate.Year).Should().AllBeEquivalentTo(DateTime.UtcNow.Year);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();

        await DbSeeder.SeedAsync(db);
        await DbSeeder.SeedAsync(db);

        (await db.Requirements.CountAsync()).Should().Be(7);
    }
}
