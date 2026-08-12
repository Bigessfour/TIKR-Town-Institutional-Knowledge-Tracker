using FluentAssertions;
using TIKR.Infrastructure.Services;
using TIKR.Infrastructure.Tests.Helpers;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;
using TIKR.Shared.TestFixtures;

namespace TIKR.Infrastructure.Tests.Services;

[Trait("Category", TestCategories.FullyTested)]
public class DashboardServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_ReturnsDueOutsWithUrgencyLabels()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Requirements.AddRange(
            Requirement("Overdue tax", today.AddDays(-3)),
            Requirement("Soon budget", today.AddDays(5)),
            Requirement("Done item", today.AddDays(2), isCompleted: true));
        await db.SaveChangesAsync();

        var sut = new DashboardService(db);
        var summary = await sut.GetSummaryAsync();

        summary.DueOuts.Should().HaveCount(2);
        summary.DueOuts.Should().Contain(d => d.Title == "Overdue tax" && d.Urgency == "Overdue");
        summary.DueOuts.Should().Contain(d => d.Title == "Soon budget" && d.Urgency == "High");
        summary.OverdueCount.Should().BeGreaterThan(0);
    }

    private static Requirement Requirement(string title, DateOnly dueDate, bool isCompleted = false) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Description = title,
        DueDate = dueDate,
        Recurrence = RecurrenceType.None,
        Category = RequirementCategory.Custom,
        IsCompleted = isCompleted
    };
}
