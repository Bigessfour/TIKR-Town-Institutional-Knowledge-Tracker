using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure.Data;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
using TIKR.Shared.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Services;

public class DashboardService(TikrDbContext db) : IDashboardService
{
    private const int DueOutWindowPastDays = 30;
    private const int DueOutCap = 25;
    private const int MissingPacketHorizonDays = 14;

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var windowStart = today.AddDays(-DueOutWindowPastDays);

        var requirements = await db.Requirements
            .AsNoTracking()
            .Where(r => !r.IsCompleted && r.DueDate >= windowStart)
            .OrderBy(r => r.DueDate)
            .Take(DueOutCap)
            .ToListAsync(cancellationToken);

        var links = await LoadRequirementLinksAsync(db);

        var dueOuts = requirements.Select(r =>
        {
            var linked = links.GetValueOrDefault(r.Id, []);
            var dto = MapRequirement(r, linked);
            var urgency = RequirementUrgencyHelper.GetUrgency(dto, today);
            return new DashboardDueOutDto(
                r.Id,
                r.Title,
                r.Description,
                r.DueDate,
                RequirementUrgencyHelper.GetLabel(urgency),
                r.SubmitTo,
                r.ContactName,
                r.ContactEmail,
                r.ContactPhone,
                r.IsCompleted,
                linked.Count,
                linked);
        }).ToList();

        var counts = CountByUrgency(dueOuts, today);
        var missingPacketCount = dueOuts.Count(d =>
            d.LinkedDocumentCount == 0
            && d.DueDate.DayNumber - today.DayNumber <= MissingPacketHorizonDays
            && d.DueDate.DayNumber - today.DayNumber >= 0);

        return new DashboardSummaryDto(
            counts.Overdue,
            counts.High,
            counts.Medium,
            counts.Low,
            missingPacketCount,
            dueOuts);
    }

    private static (int Overdue, int High, int Medium, int Low) CountByUrgency(
        IReadOnlyList<DashboardDueOutDto> dueOuts,
        DateOnly today)
    {
        var overdue = 0;
        var high = 0;
        var medium = 0;
        var low = 0;

        foreach (var item in dueOuts)
        {
            switch (item.Urgency)
            {
                case "Overdue": overdue++; break;
                case "High": high++; break;
                case "Medium": medium++; break;
                default: low++; break;
            }
        }

        return (overdue, high, medium, low);
    }

    private static async Task<Dictionary<Guid, List<RequirementLinkedDocumentDto>>> LoadRequirementLinksAsync(
        TikrDbContext db)
    {
        var rows = await db.RequirementDocuments
            .AsNoTracking()
            .Include(rd => rd.Document)
            .ToListAsync();

        return rows
            .GroupBy(row => row.RequirementId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(row => new RequirementLinkedDocumentDto(
                    row.DocumentId,
                    row.Document.FileName,
                    TruncateSummary(row.Document.FullTextContent))).ToList());
    }

    private static RequirementDto MapRequirement(
        Shared.Entities.Requirement requirement,
        IReadOnlyList<RequirementLinkedDocumentDto> linkedDocuments) =>
        new(
            requirement.Id,
            requirement.Title,
            requirement.Description,
            requirement.DueDate,
            requirement.Recurrence,
            requirement.Category,
            requirement.IsSystemSeeded,
            requirement.IsCompleted,
            linkedDocuments,
            requirement.SubmitTo,
            requirement.ContactName,
            requirement.ContactEmail,
            requirement.ContactPhone);

    private static string? TruncateSummary(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        const int max = 500;
        var normalized = text.ReplaceLineEndings(" ").Trim();
        return normalized.Length <= max ? normalized : normalized[..max] + "…";
    }
}
