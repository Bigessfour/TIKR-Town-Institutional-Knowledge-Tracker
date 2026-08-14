using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Configuration;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;
using TIKR.Shared.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Api;

internal static class CouncilPacketEndpoints
{
    private const int SummaryMaxLength = 500;

    // NOTE: GenerateCouncilPacketAsync heavy logic moved to ICouncilPacketService / CouncilPacketService
    // (in Infrastructure) for thin endpoints + proper tx/audit layering. Program.cs now delegates to service.
    // Requirement listing mappers (Load/Map/Build for packet items) kept here as used by /api/requirements GETs.

    public static async Task<Dictionary<Guid, List<RequirementLinkedDocumentDto>>> LoadRequirementLinksAsync(TikrDbContext db)
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

    public static async Task<Dictionary<Guid, (int Completed, int Total)>> LoadChecklistCountsAsync(TikrDbContext db)
    {
        var rows = await db.RequirementChecklistItems
            .AsNoTracking()
            .GroupBy(i => i.RequirementId)
            .Select(g => new
            {
                RequirementId = g.Key,
                Total = g.Count(),
                Completed = g.Count(i => i.IsCompleted)
            })
            .ToListAsync();

        return rows.ToDictionary(r => r.RequirementId, r => (r.Completed, r.Total));
    }

    public static RequirementChecklistItemDto MapChecklistItem(
        RequirementChecklistItem item,
        DateOnly parentDueDate) =>
        new(
            item.Id,
            item.RequirementId,
            item.Title,
            item.Description,
            item.IsRequired,
            item.IsCompleted,
            item.DueOffsetDays,
            item.DueDate,
            EffectiveChecklistDue(item, parentDueDate),
            item.SortOrder,
            item.LinkedDocumentId,
            item.DocumentTemplateHint,
            item.SubmitTo,
            item.ContactId);

    public static DateOnly? EffectiveChecklistDue(RequirementChecklistItem item, DateOnly parentDueDate)
    {
        if (item.DueDate is { } absolute)
            return absolute;
        if (item.DueOffsetDays is { } offset)
            return parentDueDate.AddDays(-offset);
        return null;
    }

    public static async Task<IReadOnlyList<CouncilPacketRequirementItem>> BuildCouncilPacketRequirementsAsync(TikrDbContext db)
    {
        var requirements = await db.Requirements
            .Where(r => !r.IsCompleted)
            .OrderBy(r => r.DueDate)
            .Take(50)
            .ToListAsync();

        var links = await LoadRequirementLinksAsync(db);

        return requirements.Select(requirement =>
        {
            var dto = MapRequirement(requirement, links.GetValueOrDefault(requirement.Id, []));
            var urgency = RequirementUrgencyHelper.GetUrgency(dto);
            var linked = links.GetValueOrDefault(requirement.Id, [])
                .Select(link => new CouncilPacketLinkedDocument(link.DocumentId, link.FileName, link.Summary))
                .ToList();

            return new CouncilPacketRequirementItem(
                requirement.Id,
                requirement.Title,
                requirement.Description,
                requirement.DueDate,
                requirement.Category.ToString(),
                requirement.IsCompleted ? "Completed" : "Open",
                urgency.ToString(),
                requirement.IsCompleted,
                linked);
        }).ToList();
    }

    public static RequirementDto MapRequirement(
        Requirement requirement,
        IReadOnlyList<RequirementLinkedDocumentDto> linkedDocuments,
        int checklistCompleted = 0,
        int checklistTotal = 0) =>
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
            requirement.ContactPhone,
            checklistCompleted,
            checklistTotal);

    private static string? TruncateSummary(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var normalized = text.ReplaceLineEndings(" ").Trim();
        return normalized.Length <= SummaryMaxLength
            ? normalized
            : normalized[..SummaryMaxLength] + "…";
    }
}
