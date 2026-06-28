using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TIKR.Infrastructure.Data;
using TIKR.Infrastructure.Services;
using TIKR.Infrastructure.Tests.Helpers;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;

namespace TIKR.Infrastructure.Tests.Services;

public class AuditServiceTests
{
    [Fact]
    public async Task LogAsync_PersistsAuditEntry()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = new AuditService(db);
        var entityId = Guid.NewGuid();

        await sut.LogAsync("Create", nameof(Requirement), entityId, "Test requirement");

        var log = await db.AuditLogs.SingleAsync();
        log.Action.Should().Be("Create");
        log.EntityType.Should().Be(nameof(Requirement));
        log.EntityId.Should().Be(entityId);
        log.Details.Should().Be("Test requirement");
        log.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task LogAsync_AllowsNullEntityIdAndDetails()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = new AuditService(db);

        await sut.LogAsync("Login", "User", entityId: null, details: null, userId: "clerk@town.gov");

        var log = await db.AuditLogs.SingleAsync();
        log.EntityId.Should().BeNull();
        log.Details.Should().BeNull();
        log.UserId.Should().Be("clerk@town.gov");
    }
}
