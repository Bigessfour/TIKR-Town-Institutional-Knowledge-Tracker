using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Entities;

namespace TIKR.Infrastructure;

/// <summary>
/// Seeds editable election playbook checklists for system Election requirements (idempotent per requirement).
/// </summary>
public static class RequirementChecklistSeeder
{
    public static async Task SeedAsync(TikrDbContext db, CancellationToken cancellationToken = default)
    {
        await SeedForTitleAsync(db, "Election Canvass & Certification", CanvassSteps, cancellationToken);
        await SeedForTitleAsync(db, "Campaign Finance Filing (Local)", CampaignFinanceSteps, cancellationToken);
        await SeedForTitleAsync(db, "Board Organizational Meeting", BoardOrgSteps, cancellationToken);
    }

    private static async Task SeedForTitleAsync(
        TikrDbContext db,
        string title,
        IReadOnlyList<(string Title, string? Description, int OffsetDays, string? Hint, string? SubmitTo)> steps,
        CancellationToken ct)
    {
        var requirement = await db.Requirements
            .FirstOrDefaultAsync(r => r.IsSystemSeeded && r.Title == title, ct);
        if (requirement is null)
            return;

        if (await db.RequirementChecklistItems.AnyAsync(i => i.RequirementId == requirement.Id, ct))
            return;

        var now = DateTime.UtcNow;
        var order = 0;
        foreach (var step in steps)
        {
            db.RequirementChecklistItems.Add(new RequirementChecklistItem
            {
                Id = Guid.NewGuid(),
                RequirementId = requirement.Id,
                Title = step.Title,
                Description = step.Description,
                IsRequired = true,
                IsCompleted = false,
                DueOffsetDays = step.OffsetDays,
                SortOrder = order++,
                DocumentTemplateHint = step.Hint,
                SubmitTo = step.SubmitTo,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static readonly (string Title, string? Description, int OffsetDays, string? Hint, string? SubmitTo)[] CanvassSteps =
    [
        ("Post election notice", "Confirm statutory notice / publication for the election.", 45, "election-notice.pdf", "Town board / newspaper"),
        ("Candidate filing window closed", "Verify candidate nomination petitions / write-in filings are complete.", 30, null, "Town clerk"),
        ("Assemble canvass packet", "Gather abstracts, poll books, and provisional ballots for the canvass board.", 3, "canvass-packet.pdf", "County Clerk Elections"),
        ("Hold canvass meeting", "Canvass board certifies results; record minutes.", 1, "canvass-minutes.docx", "Canvass board"),
        ("File certification with county / SOS", "Transmit certification and abstracts as required.", 0, "certification-filing.pdf", "County Clerk / SOS")
    ];

    private static readonly (string Title, string? Description, int OffsetDays, string? Hint, string? SubmitTo)[] CampaignFinanceSteps =
    [
        ("Issue candidate finance packet", "Provide local filing calendar and forms to candidates/committees.", 60, "campaign-finance-packet.pdf", "Town clerk"),
        ("Collect pre-election reports", "Receive and acknowledge candidate/committee reports due before election day.", 14, null, "Town clerk"),
        ("Post reports for public inspection", "Make filed reports available per local ordinance / CRS.", 7, null, "Town clerk"),
        ("File / archive post-election reports", "Ensure post-election and annual reports are filed and retained.", 0, "campaign-finance-report.pdf", "Town clerk")
    ];

    private static readonly (string Title, string? Description, int OffsetDays, string? Hint, string? SubmitTo)[] BoardOrgSteps =
    [
        ("Confirm election certification on file", "Organizational meeting requires certified election results.", 14, null, "Town clerk"),
        ("Prepare oaths and officer slate", "Draft oaths of office and proposed mayor/mayor pro tem nominations.", 7, "oath-of-office.pdf", "Town board"),
        ("Post organizational agenda", "Publish agenda with officer elections and meeting schedule.", 3, "org-meeting-agenda.pdf", "Town clerk"),
        ("Hold organizational meeting", "Elect officers, adopt meeting schedule, assign committees.", 0, "org-meeting-minutes.docx", "Town board"),
        ("Update posting places / bank signatories", "Refresh notice board list and financial authorizations if officers change.", -7, null, "Town clerk / finance")
    ];
}
