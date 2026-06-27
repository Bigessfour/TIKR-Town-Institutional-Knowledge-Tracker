using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TIKR.Api.Tests.Fixtures;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;

namespace TIKR.Api.Tests.Endpoints;

public class AuditEndpointTests : IClassFixture<TikrWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuditEndpointTests(TikrWebApplicationFactory factory) =>
        _client = factory.CreateClient();

    [Fact]
    public async Task GetAudit_ReturnsNewestFirstWithLimit()
    {
        await _client.PostAsJsonAsync("/api/knowledge", new CreateKnowledgeEntryRequest(
            "Audit trail test",
            "Content",
            KnowledgeCategory.HowTo,
            SortOrder: 0));

        var logs = await _client.GetFromJsonAsync<List<AuditLogDto>>("/api/audit?limit=3");
        logs.Should().NotBeNull();
        logs!.Should().HaveCountLessThanOrEqualTo(3);
        logs.Should().BeInDescendingOrder(a => a.Timestamp);
        logs.Should().Contain(a => a.Action == "Create" && a.EntityType == "KnowledgeEntry");
    }

    internal sealed record AuditLogDto(
        Guid Id,
        string Action,
        string EntityType,
        Guid? EntityId,
        string? Details,
        DateTime Timestamp);
}
