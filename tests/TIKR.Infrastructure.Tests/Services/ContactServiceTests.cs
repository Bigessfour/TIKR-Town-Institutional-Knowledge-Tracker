using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TIKR.Infrastructure.Services;
using TIKR.Infrastructure.Tests.Helpers;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Tests.Services;

public class ContactServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsContactAndAudits()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = new ContactService(db);
        var audit = new Mock<IAuditService>();
        var user = Mock.Of<ICurrentUserService>(u => u.UserId == "deb");

        var created = await sut.CreateAsync(
            new CreateContactRequest(
                "Election – County Clerk",
                Role: "County Clerk",
                Organization: "County Clerk's Office",
                Address: "Courthouse",
                Office: "Elections",
                Email: "clerk@county.example.gov",
                Phone: "970-555-0100",
                Categories: ContactCategory.Election),
            audit.Object,
            user);

        created.Name.Should().Be("Election – County Clerk");
        created.Categories.Should().Be(ContactCategory.Election);
        created.CreatedBy.Should().Be("deb");

        var stored = await db.Contacts.SingleAsync();
        stored.Email.Should().Be("clerk@county.example.gov");
        audit.Verify(a => a.LogAsync(
            "Create", nameof(Contact), created.Id, created.Name, "deb", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteAndRestore_RoundTrip()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = new ContactService(db);
        var audit = Mock.Of<IAuditService>();
        var user = Mock.Of<ICurrentUserService>(u => u.UserId == "deb");

        var created = await sut.CreateAsync(
            new CreateContactRequest("Temp POC", Categories: ContactCategory.Custom),
            audit,
            user);

        await sut.SoftDeleteAsync(created.Id, audit, user);
        (await sut.ListAsync()).Should().BeEmpty();
        (await sut.ListAsync(deleted: true)).Should().ContainSingle(c => c.Id == created.Id);

        var restored = await sut.RestoreAsync(created.Id, audit, user);
        restored.DeletedAt.Should().BeNull();
        (await sut.ListAsync()).Should().ContainSingle(c => c.Id == created.Id);
    }

    [Fact]
    public async Task LinkToRequirementAsync_SetsPrimaryAndDenormalizedFields()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = new ContactService(db);
        var audit = Mock.Of<IAuditService>();
        var user = Mock.Of<ICurrentUserService>(u => u.UserId == "deb");

        var requirement = new Requirement
        {
            Id = Guid.NewGuid(),
            Title = "Election Canvass & Certification",
            DueDate = new DateOnly(2026, 11, 15),
            Category = RequirementCategory.Election
        };
        db.Requirements.Add(requirement);
        await db.SaveChangesAsync();

        var contact = await sut.CreateAsync(
            new CreateContactRequest(
                "Election – County Clerk",
                Email: "elections@county.example.gov",
                Phone: "970-555-0100",
                Categories: ContactCategory.Election),
            audit,
            user);

        await sut.LinkToRequirementAsync(requirement.Id, contact.Id, isPrimary: true, audit, user);

        var reloaded = await db.Requirements.SingleAsync(r => r.Id == requirement.Id);
        reloaded.ContactName.Should().Be("Election – County Clerk");
        reloaded.ContactEmail.Should().Be("elections@county.example.gov");
        reloaded.ContactPhone.Should().Be("970-555-0100");

        var linked = await sut.ListForRequirementAsync(requirement.Id);
        linked.Should().ContainSingle(x => x.Contact.Id == contact.Id && x.IsPrimary);
    }

    [Fact]
    public async Task UnlinkFromRequirementAsync_RemovesJunction()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = new ContactService(db);
        var audit = Mock.Of<IAuditService>();
        var user = Mock.Of<ICurrentUserService>(u => u.UserId == "deb");

        var requirement = new Requirement
        {
            Id = Guid.NewGuid(),
            Title = "Test",
            DueDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Category = RequirementCategory.Custom
        };
        db.Requirements.Add(requirement);
        await db.SaveChangesAsync();

        var contact = await sut.CreateAsync(
            new CreateContactRequest("POC", Categories: ContactCategory.Custom),
            audit,
            user);
        await sut.LinkToRequirementAsync(requirement.Id, contact.Id, isPrimary: true, audit, user);
        await sut.UnlinkFromRequirementAsync(requirement.Id, contact.Id, audit, user);

        (await sut.ListForRequirementAsync(requirement.Id)).Should().BeEmpty();
    }
}
