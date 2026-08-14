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

public class RequirementChecklistServiceTests
{
    [Fact]
    public async Task Checklist_CrudCompleteAndReorder_AuditsMutations()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = new RequirementService(db);
        var audit = new Mock<IAuditService>();
        var user = Mock.Of<ICurrentUserService>(u => u.UserId == "deb");

        var requirement = await sut.CreateAsync(
            new CreateRequirementRequest(
                "Custom election prep",
                "Local playbook",
                new DateOnly(2026, 11, 15),
                RecurrenceType.None,
                RequirementCategory.Election),
            audit.Object,
            user);

        var first = await sut.AddChecklistItemAsync(
            requirement.Id,
            new CreateRequirementChecklistItemRequest("Post notice", DueOffsetDays: 45),
            audit.Object,
            user);
        var second = await sut.AddChecklistItemAsync(
            requirement.Id,
            new CreateRequirementChecklistItemRequest("File certification", DueOffsetDays: 0),
            audit.Object,
            user);

        (await sut.ListChecklistAsync(requirement.Id)).Should().HaveCount(2);

        await sut.CompleteChecklistItemAsync(requirement.Id, first.Id, true, audit.Object, user);
        (await db.RequirementChecklistItems.SingleAsync(i => i.Id == first.Id)).IsCompleted.Should().BeTrue();

        await sut.ReorderChecklistAsync(requirement.Id, [second.Id, first.Id], audit.Object, user);
        var ordered = await sut.ListChecklistAsync(requirement.Id);
        ordered[0].Id.Should().Be(second.Id);
        ordered[0].SortOrder.Should().Be(0);
        ordered[1].Id.Should().Be(first.Id);

        await sut.DeleteChecklistItemAsync(requirement.Id, second.Id, audit.Object, user);
        (await sut.ListChecklistAsync(requirement.Id)).Should().ContainSingle(i => i.Id == first.Id);

        audit.Verify(a => a.LogAsync(
            "ChecklistCreate", nameof(Requirement), requirement.Id, It.IsAny<string?>(), "deb", It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        audit.Verify(a => a.LogAsync(
            "ChecklistComplete", nameof(Requirement), requirement.Id, "Post notice", "deb", It.IsAny<CancellationToken>()),
            Times.Once);
        audit.Verify(a => a.LogAsync(
            "ChecklistReorder", nameof(Requirement), requirement.Id, It.IsAny<string?>(), "deb", It.IsAny<CancellationToken>()),
            Times.Once);
        audit.Verify(a => a.LogAsync(
            "ChecklistDelete", nameof(Requirement), requirement.Id, "File certification", "deb", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateChecklistItemAsync_PersistsFieldsAndAudits()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = new RequirementService(db);
        var audit = new Mock<IAuditService>();
        var user = Mock.Of<ICurrentUserService>(u => u.UserId == "deb");

        var requirement = await sut.CreateAsync(
            new CreateRequirementRequest(
                "Editable playbook host",
                null,
                new DateOnly(2026, 11, 15),
                RecurrenceType.None,
                RequirementCategory.Election),
            audit.Object,
            user);
        var item = await sut.AddChecklistItemAsync(
            requirement.Id,
            new CreateRequirementChecklistItemRequest("Post notice", DueOffsetDays: 45),
            audit.Object,
            user);

        var updated = await sut.UpdateChecklistItemAsync(
            requirement.Id,
            item.Id,
            new UpdateRequirementChecklistItemRequest(
                "Post statutory notice",
                Description: "Confirm publication window",
                IsRequired: true,
                IsCompleted: false,
                DueOffsetDays: 40,
                SortOrder: item.SortOrder,
                SubmitTo: "Town board / newspaper"),
            audit.Object,
            user);

        updated.Title.Should().Be("Post statutory notice");
        updated.Description.Should().Be("Confirm publication window");
        updated.DueOffsetDays.Should().Be(40);
        updated.SubmitTo.Should().Be("Town board / newspaper");

        var stored = await db.RequirementChecklistItems.SingleAsync(i => i.Id == item.Id);
        stored.Title.Should().Be("Post statutory notice");
        stored.DueOffsetDays.Should().Be(40);

        audit.Verify(a => a.LogAsync(
            "ChecklistUpdate", nameof(Requirement), requirement.Id, It.IsAny<string?>(), "deb", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteRequirement_CascadesChecklistItems()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = new RequirementService(db);
        var audit = Mock.Of<IAuditService>();
        var user = Mock.Of<ICurrentUserService>(u => u.UserId == "deb");

        var requirement = await sut.CreateAsync(
            new CreateRequirementRequest(
                "Temp playbook",
                null,
                new DateOnly(2026, 6, 1),
                RecurrenceType.None,
                RequirementCategory.Custom),
            audit,
            user);
        await sut.AddChecklistItemAsync(
            requirement.Id,
            new CreateRequirementChecklistItemRequest("Step one"),
            audit,
            user);

        await sut.DeleteAsync(requirement.Id, audit, user);

        (await db.RequirementChecklistItems.CountAsync(i => i.RequirementId == requirement.Id)).Should().Be(0);
    }
}
