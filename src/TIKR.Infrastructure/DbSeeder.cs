using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;

namespace TIKR.Infrastructure;

public static class DbSeeder
{
    public static async Task SeedAsync(TikrDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Requirements.AnyAsync(r => r.IsSystemSeeded, cancellationToken))
            return;

        var year = DateTime.UtcNow.Year;
        var deadlines = new List<Requirement>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Annual Budget Submission",
                Description = "Submit the town's annual budget to the Board of Trustees per C.R.S. § 29-1-109.",
                DueDate = new DateOnly(year, 1, 31),
                Recurrence = RecurrenceType.Annual,
                Category = RequirementCategory.Budget,
                IsSystemSeeded = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Audit Exemption Filing",
                Description = "File audit exemption petition if applicable (towns under $750K revenue).",
                DueDate = new DateOnly(year, 3, 31),
                Recurrence = RecurrenceType.Annual,
                Category = RequirementCategory.Audit,
                IsSystemSeeded = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Annual Financial Audit Due",
                Description = "Complete annual audit and file with Division of Local Government.",
                DueDate = new DateOnly(year, 7, 31),
                Recurrence = RecurrenceType.Annual,
                Category = RequirementCategory.Audit,
                IsSystemSeeded = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "TABOR Revenue Report",
                Description = "Prepare TABOR revenue and spending report for public inspection.",
                DueDate = new DateOnly(year, 9, 30),
                Recurrence = RecurrenceType.Annual,
                Category = RequirementCategory.Compliance,
                IsSystemSeeded = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Mill Levy Certification",
                Description = "Certify mill levy to county assessor by December 15.",
                DueDate = new DateOnly(year, 12, 15),
                Recurrence = RecurrenceType.Annual,
                Category = RequirementCategory.MillLevy,
                IsSystemSeeded = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Election Canvass & Certification",
                Description = "Canvass election results and file certification documents.",
                DueDate = new DateOnly(year, 11, 15),
                Recurrence = RecurrenceType.Annual,
                Category = RequirementCategory.Election,
                IsSystemSeeded = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Quarterly Financial Report",
                Description = "Prepare quarterly financial summary for board meeting.",
                DueDate = new DateOnly(year, 3, 31),
                Recurrence = RecurrenceType.Quarterly,
                Category = RequirementCategory.Budget,
                IsSystemSeeded = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Periodic Financial Report (PFR)",
                Description = "File Periodic Financial Report with Division of Local Government per C.R.S. § 29-1-503.",
                DueDate = new DateOnly(year, 1, 31),
                Recurrence = RecurrenceType.Annual,
                Category = RequirementCategory.Budget,
                IsSystemSeeded = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Sales Tax Return to DOR",
                Description = "Remit municipal sales tax collections to Colorado Department of Revenue.",
                DueDate = new DateOnly(year, DateTime.UtcNow.Month, DateTime.DaysInMonth(year, DateTime.UtcNow.Month)),
                Recurrence = RecurrenceType.Monthly,
                Category = RequirementCategory.Compliance,
                IsSystemSeeded = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Open Meetings Law Compliance Review",
                Description = "Review posted agendas, minutes retention, and C.R.S. § 24-6-402 compliance.",
                DueDate = new DateOnly(year, 6, 30),
                Recurrence = RecurrenceType.Annual,
                Category = RequirementCategory.Compliance,
                IsSystemSeeded = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Municipal Court Statistics Report",
                Description = "Submit annual municipal court statistics to the Colorado Judicial Branch.",
                DueDate = new DateOnly(year, 1, 15),
                Recurrence = RecurrenceType.Annual,
                Category = RequirementCategory.Compliance,
                IsSystemSeeded = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Board Organizational Meeting",
                Description = "Hold organizational meeting after regular municipal election; elect officers and set meeting schedule.",
                DueDate = new DateOnly(year, 5, 15),
                Recurrence = RecurrenceType.Annual,
                Category = RequirementCategory.Election,
                IsSystemSeeded = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Campaign Finance Filing (Local)",
                Description = "File local candidate and committee campaign finance reports with the town clerk.",
                DueDate = new DateOnly(year, 3, 15),
                Recurrence = RecurrenceType.Annual,
                Category = RequirementCategory.Election,
                IsSystemSeeded = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Liquor License Renewal Notice",
                Description = "Send renewal notices and collect fees for active liquor licenses before expiration.",
                DueDate = new DateOnly(year, 4, 1),
                Recurrence = RecurrenceType.Annual,
                Category = RequirementCategory.Compliance,
                IsSystemSeeded = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Special District Intergovernmental Agreement Review",
                Description = "Review IGA obligations with fire, water, or metro districts; confirm billing and service terms.",
                DueDate = new DateOnly(year, 9, 1),
                Recurrence = RecurrenceType.Annual,
                Category = RequirementCategory.Compliance,
                IsSystemSeeded = true
            }
        };

        db.Requirements.AddRange(deadlines);
        await db.SaveChangesAsync(cancellationToken);
    }
}
