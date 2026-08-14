using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure.Data;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;

namespace TIKR.Infrastructure;

public static class ContactSeeder
{
    public static async Task SeedAsync(TikrDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.Contacts.AnyAsync(cancellationToken))
            return;

        var countyClerkId = Guid.NewGuid();
        var sosId = Guid.NewGuid();
        var canvassId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var contacts = new List<Contact>
        {
            new()
            {
                Id = countyClerkId,
                Name = "Election – County Clerk",
                Role = "County Clerk and Recorder",
                Organization = "County Clerk's Office",
                Address = "County Courthouse, Main Street",
                Office = "Elections Division",
                Email = "elections@county.example.gov",
                Phone = "970-555-0100",
                Notes = "Primary POC for canvass packets and certification filing.",
                Categories = ContactCategory.Election,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "system"
            },
            new()
            {
                Id = sosId,
                Name = "Colorado Secretary of State – Elections",
                Role = "Elections Division",
                Organization = "Colorado Department of State",
                Address = "1700 Broadway, Denver, CO 80290",
                Office = "Elections",
                Email = "elections@coloradosos.gov",
                Phone = "303-894-2200",
                Notes = "State elections guidance and form references.",
                Categories = ContactCategory.Election,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "system"
            },
            new()
            {
                Id = canvassId,
                Name = "County Canvass Board Chair",
                Role = "Canvass Board Chair",
                Organization = "County Canvass Board",
                Address = null,
                Office = "Elections",
                Email = "canvass@county.example.gov",
                Phone = "970-555-0110",
                Notes = "Coordinate canvass meeting schedule after election day.",
                Categories = ContactCategory.Election,
                CreatedAt = now,
                UpdatedAt = now,
                CreatedBy = "system"
            }
        };

        db.Contacts.AddRange(contacts);
        await db.SaveChangesAsync(cancellationToken);

        var canvassRequirement = await db.Requirements
            .FirstOrDefaultAsync(r => r.IsSystemSeeded && r.Title == "Election Canvass & Certification", cancellationToken);

        if (canvassRequirement is null)
            return;

        var clerk = contacts[0];
        canvassRequirement.ContactName = clerk.Name;
        canvassRequirement.ContactEmail = clerk.Email;
        canvassRequirement.ContactPhone = clerk.Phone;
        canvassRequirement.SubmitTo ??= "County Clerk's Office";
        canvassRequirement.UpdatedAt = now;

        db.RequirementContacts.Add(new RequirementContact
        {
            RequirementId = canvassRequirement.Id,
            ContactId = countyClerkId,
            LinkedAt = now,
            IsPrimary = true
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}
