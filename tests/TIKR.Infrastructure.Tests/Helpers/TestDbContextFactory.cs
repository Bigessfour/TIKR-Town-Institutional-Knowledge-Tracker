using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure;
using TIKR.Infrastructure.Data;

namespace TIKR.Infrastructure.Tests.Helpers;

public static class TestDbContextFactory
{
    public static async Task<TikrDbContext> CreateMigratedAsync(string? databasePath = null)
    {
        databasePath ??= Path.Combine(Path.GetTempPath(), $"tikr-test-{Guid.NewGuid():N}.db");

        var options = new DbContextOptionsBuilder<TikrDbContext>()
            .UseSqlite($"Data Source={databasePath}")
            .Options;

        var context = new TikrDbContext(options);
        await context.Database.MigrateAsync();
        return context;
    }
}
