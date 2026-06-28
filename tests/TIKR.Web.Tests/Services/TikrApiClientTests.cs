using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using TIKR.Shared.Constants;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
using TIKR.Shared.TestFixtures;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Services;

public class TikrApiClientTests
{
    [Fact]
    public async Task GetDashboardPrioritiesAsync_DeserializesResponse()
    {
        var json = JsonSerializer.Serialize(new List<DashboardPriority>
        {
            new("Budget", "Due soon", DateOnly.FromDateTime(DateTime.UtcNow), "High")
        });
        var (client, _) = CreateClient(json, HttpMethod.Get, "/api/ai/dashboard-priorities");
        var sut = new TikrApiClient(client);

        var items = await sut.GetDashboardPrioritiesAsync();
        items.Should().ContainSingle().Which.Title.Should().Be("Budget");
    }

    [Fact]
    public async Task GetRequirementsAsync_DeserializesResponse()
    {
        var id = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new List<RequirementDto>
        {
            new(id, "Budget", null, DateOnly.FromDateTime(DateTime.UtcNow), RecurrenceType.Annual,
                RequirementCategory.Budget, true, false)
        });
        var (client, _) = CreateClient(json, HttpMethod.Get, "/api/requirements");
        var sut = new TikrApiClient(client);

        var items = await sut.GetRequirementsAsync();
        items.Should().ContainSingle().Which.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetRequirementAsync_DeserializesSingleItem()
    {
        var id = Guid.NewGuid();
        var json = JsonSerializer.Serialize(
            new RequirementDto(id, "One", null, DateOnly.FromDateTime(DateTime.UtcNow), RecurrenceType.None,
                RequirementCategory.Custom, false, false));
        var (client, _) = CreateClient(json, HttpMethod.Get, $"/api/requirements/{id}");
        var sut = new TikrApiClient(client);

        var item = await sut.GetRequirementAsync(id);
        item!.Title.Should().Be("One");
    }

    [Fact]
    public async Task UpdateRequirementAsync_SendsPut()
    {
        HttpMethod? method = null;
        var handler = new RecordingHandler((req, _) =>
        {
            method = req.Method;
            return JsonResponse("{}");
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var sut = new TikrApiClient(client);
        var id = Guid.NewGuid();

        await sut.UpdateRequirementAsync(id, new UpdateRequirementRequest(
            "T", null, DateOnly.FromDateTime(DateTime.UtcNow), RecurrenceType.None, RequirementCategory.Custom, false));

        method.Should().Be(HttpMethod.Put);
    }

    [Fact]
    public async Task GetDocumentsAsync_AppendsSearchQuery()
    {
        string? path = null;
        var handler = new RecordingHandler((req, _) =>
        {
            path = req.RequestUri!.PathAndQuery;
            return JsonResponse("[]");
        });
        var sut = new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        await sut.GetDocumentsAsync("minutes");
        path.Should().Be("/api/documents?q=minutes");
    }

    [Fact]
    public async Task GetDocumentsAsync_WithoutQuery_UsesBasePath()
    {
        string? path = null;
        var handler = new RecordingHandler((req, _) =>
        {
            path = req.RequestUri!.PathAndQuery;
            return JsonResponse("[]");
        });
        var sut = new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        await sut.GetDocumentsAsync();
        path.Should().Be("/api/documents");
    }

    [Fact]
    public async Task GetKnowledgeEntriesAsync_DeserializesResponse()
    {
        var id = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new List<KnowledgeEntryDto>
        {
            new(id, "How to", "content", KnowledgeCategory.HowTo, 0)
        });
        var (client, _) = CreateClient(json, HttpMethod.Get, "/api/knowledge");
        var sut = new TikrApiClient(client);

        (await sut.GetKnowledgeEntriesAsync()).Should().ContainSingle().Which.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetAiStatusAsync_DeserializesResponse()
    {
        var json = """{"ollamaAvailable":true,"ollamaModel":"llama3.2:3b","grokEnabled":false}""";
        var (client, _) = CreateClient(json, HttpMethod.Get, "/api/ai/status");
        var sut = new TikrApiClient(client);

        var status = await sut.GetAiStatusAsync();
        status!.OllamaAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task UploadDocumentAsync_PostsMultipart()
    {
        HttpMethod? method = null;
        var handler = new RecordingHandler((req, _) =>
        {
            method = req.Method;
            return new HttpResponseMessage(HttpStatusCode.Created);
        });
        var sut = new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        await using var stream = new MemoryStream("bytes"u8.ToArray());
        await sut.UploadDocumentAsync(stream, "file.pdf");

        method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task TagDocumentAsync_ReturnsNullOnFailure()
    {
        var handler = new RecordingHandler((_, _) => new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        (await sut.TagDocumentAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task TagDocumentAsync_DeserializesSuccess()
    {
        var docId = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new TagDocumentResponse(docId, ["budget"], "Finance"));
        var (client, _) = CreateClient(json, HttpMethod.Post, "/api/ai/tag-document");
        var sut = new TikrApiClient(client);

        var result = await sut.TagDocumentAsync(docId);
        result!.SuggestedFolder.Should().Be("Finance");
    }

    [Fact]
    public async Task AskAdvancedAsync_ReturnsNullOnFailure()
    {
        var handler = new RecordingHandler((_, _) => new HttpResponseMessage(HttpStatusCode.BadRequest));
        var sut = new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        (await sut.AskAdvancedAsync("hello")).Should().BeNull();
    }

    [Fact]
    public async Task CreateRequirementAsync_PostsJson()
    {
        HttpMethod? method = null;
        var handler = new RecordingHandler((req, _) =>
        {
            method = req.Method;
            return new HttpResponseMessage(HttpStatusCode.Created);
        });
        var sut = new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        await sut.CreateRequirementAsync(new CreateRequirementRequest(
            "T", null, DateOnly.FromDateTime(DateTime.UtcNow), RecurrenceType.None, RequirementCategory.Custom));

        method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task CreateKnowledgeEntryAsync_ReturnsCreatedEntry()
    {
        var id = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new KnowledgeEntryDto(id, "Voice", "text", KnowledgeCategory.TribalKnowledge, 1));
        var (client, _) = CreateClient(json, HttpMethod.Post, "/api/knowledge");
        var sut = new TikrApiClient(client);

        var created = await sut.CreateKnowledgeEntryAsync(new CreateKnowledgeEntryRequest("Voice", "text", KnowledgeCategory.TribalKnowledge, 1));
        created!.Id.Should().Be(id);
    }

    [Fact]
    public async Task UpdateKnowledgeEntryAsync_SendsPut()
    {
        HttpMethod? method = null;
        var handler = new RecordingHandler((req, _) =>
        {
            method = req.Method;
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var sut = new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        await sut.UpdateKnowledgeEntryAsync(Guid.NewGuid(), new UpdateKnowledgeEntryRequest("T", "C", KnowledgeCategory.HowTo, 0));
        method.Should().Be(HttpMethod.Put);
    }

    [Fact]
    public async Task DeleteKnowledgeEntryAsync_SendsDelete()
    {
        HttpMethod? method = null;
        var handler = new RecordingHandler((req, _) =>
        {
            method = req.Method;
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var sut = new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        await sut.DeleteKnowledgeEntryAsync(Guid.NewGuid());
        method.Should().Be(HttpMethod.Delete);
    }

    [Fact]
    public async Task DeleteRequirementAsync_SendsDelete()
    {
        HttpMethod? method = null;
        var handler = new RecordingHandler((req, _) =>
        {
            method = req.Method;
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var sut = new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        await sut.DeleteRequirementAsync(Guid.NewGuid());
        method.Should().Be(HttpMethod.Delete);
    }

    [Fact]
    public async Task GetRecentAuditAsync_AppendsLimit()
    {
        string? path = null;
        var handler = new RecordingHandler((req, _) =>
        {
            path = req.RequestUri!.PathAndQuery;
            return JsonResponse("[]");
        });
        var sut = new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        await sut.GetRecentAuditAsync(25);
        path.Should().Be("/api/audit?limit=25");
    }

    [Fact]
    public async Task DeleteDocumentAsync_SendsDelete()
    {
        HttpMethod? method = null;
        var handler = new RecordingHandler((req, _) =>
        {
            method = req.Method;
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var sut = new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        await sut.DeleteDocumentAsync(Guid.NewGuid());
        method.Should().Be(HttpMethod.Delete);
    }

    [Fact]
    public void GetDocumentContentUrl_BuildsPath()
    {
        var id = Guid.NewGuid();
        var sut = new TikrApiClient(new HttpClient { BaseAddress = new Uri("http://localhost/") });
        sut.GetDocumentContentUrl(id).Should().Be($"/api/documents/{id}/content");
    }

    [Fact]
    public async Task SemanticSearchDocumentsAsync_DeserializesResponse()
    {
        var json = JsonSerializer.Serialize(new SemanticSearchResponse("budget", 1, [
            new SemanticSearchHit(Guid.NewGuid(), "a.pdf", "Finance", "snippet", 0.9)
        ]));
        var (client, _) = CreateClient(json, HttpMethod.Post, "/api/ai/semantic-search");
        var sut = new TikrApiClient(client);

        var result = await sut.SemanticSearchDocumentsAsync("budget");
        result!.Hits.Should().ContainSingle();
    }

    [Fact]
    public async Task EmbedDocumentAsync_ReturnsNullOnFailure()
    {
        var handler = new RecordingHandler((_, _) => new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        (await sut.EmbedDocumentAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task SemanticSearchKnowledgeAsync_DeserializesResponse()
    {
        var json = JsonSerializer.Serialize(new SemanticSearchKnowledgeResponse("permit", 1, [
            new SemanticSearchKnowledgeHit(Guid.NewGuid(), "Entry", "HowTo", "snippet", 0.8)
        ]));
        var (client, _) = CreateClient(json, HttpMethod.Post, "/api/ai/semantic-search-knowledge");
        var sut = new TikrApiClient(client);

        var result = await sut.SemanticSearchKnowledgeAsync("permit");
        result!.Hits.Should().ContainSingle();
    }

    [Fact]
    public async Task EmbedKnowledgeEntryAsync_DeserializesSuccess()
    {
        var entryId = Guid.NewGuid();
        var json = JsonSerializer.Serialize(new EmbedKnowledgeEntryResponse(entryId, true, null));
        var (client, _) = CreateClient(json, HttpMethod.Post, $"/api/ai/embed-knowledge/{entryId}");
        var sut = new TikrApiClient(client);

        var result = await sut.EmbedKnowledgeEntryAsync(entryId);
        result!.Embedded.Should().BeTrue();
    }

    [Fact]
    public async Task GetProfileAsync_DeserializesResponse()
    {
        var json = JsonSerializer.Serialize(new UserProfileDto("user-1", "clerk@town.gov", "Clerk", [TikrRoles.Clerk]));
        var (client, _) = CreateClient(json, HttpMethod.Get, "/api/auth/me");
        var sut = new TikrApiClient(client);

        var profile = await sut.GetProfileAsync();
        profile!.Email.Should().Be("clerk@town.gov");
    }

    [Fact]
    public async Task GetUsersAsync_DeserializesResponse()
    {
        var json = JsonSerializer.Serialize(new List<UserSummaryDto>
        {
            new("user-1", "admin@town.gov", "Admin", true, [TikrRoles.Admin])
        });
        var (client, _) = CreateClient(json, HttpMethod.Get, "/api/auth/users");
        var sut = new TikrApiClient(client);

        (await sut.GetUsersAsync()).Should().ContainSingle().Which.Email.Should().Be("admin@town.gov");
    }

    [Fact]
    public async Task CreateUserAsync_ReturnsNullOnFailure()
    {
        var handler = new RecordingHandler((_, _) => new HttpResponseMessage(HttpStatusCode.BadRequest));
        var sut = new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        var result = await sut.CreateUserAsync(new CreateUserRequest(
            "new@town.gov", TestAuthFixtures.NewUserPassword, "New Clerk", TikrRoles.Clerk));

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateUserAsync_DeserializesSuccess()
    {
        var json = JsonSerializer.Serialize(new UserSummaryDto(
            "user-2", "new@town.gov", "New Clerk", true, [TikrRoles.Clerk]));
        var (client, _) = CreateClient(json, HttpMethod.Post, "/api/auth/users");
        var sut = new TikrApiClient(client);

        var created = await sut.CreateUserAsync(new CreateUserRequest(
            "new@town.gov", TestAuthFixtures.NewUserPassword, "New Clerk", TikrRoles.Clerk));

        created!.Email.Should().Be("new@town.gov");
    }

    [Fact]
    public async Task UpdateUserAsync_DeserializesSuccess()
    {
        var json = JsonSerializer.Serialize(new UserSummaryDto(
            "user-2", "new@town.gov", "New Clerk", false, [TikrRoles.Clerk]));
        var (client, _) = CreateClient(json, HttpMethod.Put, "/api/auth/users/user-2");
        var sut = new TikrApiClient(client);

        var updated = await sut.UpdateUserAsync("user-2", new UpdateUserRequest(false, null, TikrRoles.Clerk));
        updated!.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsTrueOnSuccess()
    {
        var handler = new RecordingHandler((req, _) =>
        {
            req.Method.Should().Be(HttpMethod.Post);
            req.RequestUri!.PathAndQuery.Should().Be("/api/auth/change-password");
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var sut = new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        var ok = await sut.ChangePasswordAsync(new ChangePasswordRequest(
            TestAuthFixtures.BootstrapPassword, TestAuthFixtures.NewUserPassword));
        ok.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePasswordAsync_ReturnsFalseOnFailure()
    {
        var handler = new RecordingHandler((_, _) => new HttpResponseMessage(HttpStatusCode.BadRequest));
        var sut = new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        var ok = await sut.ChangePasswordAsync(new ChangePasswordRequest(TestAuthFixtures.BootstrapPassword, "weak"));
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task ScanDocumentWithAgentAsync_PostsMultipart()
    {
        var json = JsonSerializer.Serialize(new DocumentAgentResult(
            "Budget report", "text", DateOnly.FromDateTime(DateTime.UtcNow),
            RecurrenceType.Annual, RequirementCategory.Budget, 2, "agent/x.pdf", true));
        HttpMethod? method = null;
        var handler = new RecordingHandler((req, _) =>
        {
            method = req.Method;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });
        var sut = new TikrApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        await using var stream = new MemoryStream("bytes"u8.ToArray());
        var result = await sut.ScanDocumentWithAgentAsync(stream, "budget.pdf");

        method.Should().Be(HttpMethod.Post);
        result!.SuggestedCategory.Should().Be(RequirementCategory.Budget);
    }

    private static (HttpClient Client, RecordingHandler Handler) CreateClient(string json, HttpMethod expectedMethod, string expectedPath)
    {
        HttpMethod? method = null;
        string? path = null;
        var handler = new RecordingHandler((request, _) =>
        {
            method = request.Method;
            path = request.RequestUri!.PathAndQuery;
            return JsonResponse(json);
        });
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        _ = method;
        _ = path;
        return (client, handler);
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class RecordingHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(handler(request, cancellationToken));
    }
}
