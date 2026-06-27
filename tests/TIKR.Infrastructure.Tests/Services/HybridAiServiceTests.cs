using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TIKR.Infrastructure.Data;
using TIKR.Infrastructure.Services;
using TIKR.Infrastructure.Tests.Helpers;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;

namespace TIKR.Infrastructure.Tests.Services;

public class HybridAiServiceTests
{
    private static readonly GrokService DisabledGrok = TestGrokServiceFactory.CreateDisabled();

    [Fact]
    public async Task TagDocumentAsync_ThrowsWhenDocumentMissing()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = CreateService(db, Mock.Of<IOllamaChatClientFactory>(), DisabledGrok);

        var act = async () => await sut.TagDocumentAsync(Guid.NewGuid());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task TagDocumentAsync_ParsesJsonAndPersistsTags()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = "budget.pdf",
            StoragePath = "2026/01/budget.pdf",
            ContentType = "application/pdf",
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Documents.Add(document);
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactory("""
            Here is the analysis:
            {"tags":["budget","finance"],"suggestedFolder":"Finance"}
            """);

        var sut = CreateService(db, ollama, DisabledGrok);
        var result = await sut.TagDocumentAsync(document.Id);

        result.Tags.Should().BeEquivalentTo(["budget", "finance"]);
        result.SuggestedFolder.Should().Be("Finance");

        var updated = await db.Documents.FindAsync(document.Id);
        updated!.SuggestedFolder.Should().Be("Finance");
    }

    [Fact]
    public async Task TagDocumentAsync_UsesFallbackTagsOnMalformedJson()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = "notes.txt",
            StoragePath = "2026/01/notes.txt",
            ContentType = "text/plain",
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Documents.Add(document);
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactory("not valid json at all");
        var sut = CreateService(db, ollama, DisabledGrok);

        var result = await sut.TagDocumentAsync(document.Id);
        result.Tags.Should().BeEquivalentTo(["uncategorized"]);
    }

    [Fact]
    public async Task TagDocumentAsync_WhenOllamaReturnsEmpty_LeavesTagsEmpty()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = "scan.pdf",
            StoragePath = "2026/01/scan.pdf",
            ContentType = "application/pdf",
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Documents.Add(document);
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactory("");
        var sut = CreateService(db, ollama, DisabledGrok);

        var result = await sut.TagDocumentAsync(document.Id);
        result.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDashboardPrioritiesAsync_BucketsByDueDate()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        db.Requirements.AddRange(
            Requirement("Overdue item", today.AddDays(-2)),
            Requirement("Soon item", today.AddDays(7)),
            Requirement("Later item", today.AddDays(45)));
        await db.SaveChangesAsync();

        var sut = CreateService(db, Mock.Of<IOllamaChatClientFactory>(), DisabledGrok);
        var priorities = await sut.GetDashboardPrioritiesAsync();

        priorities.Should().Contain(p => p.Title == "Overdue item" && p.Priority == "Overdue");
        priorities.Should().Contain(p => p.Title == "Soon item" && p.Priority == "High");
        priorities.Should().Contain(p => p.Title == "Later item" && p.Priority == "Low");
    }

    [Fact]
    public async Task GetDashboardPrioritiesAsync_ReturnsPlaceholderWhenEmpty()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = CreateService(db, Mock.Of<IOllamaChatClientFactory>(), DisabledGrok);

        var priorities = await sut.GetDashboardPrioritiesAsync();

        priorities.Should().ContainSingle()
            .Which.Title.Should().Be("No urgent deadlines");
    }

    [Fact]
    public async Task AskAdvancedAsync_ThrowsWhenGrokDisabled()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = CreateService(db, Mock.Of<IOllamaChatClientFactory>(), DisabledGrok);

        var act = async () => await sut.AskAdvancedAsync(new AskAdvancedRequest("hello", null));
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task AskAdvancedAsync_ReturnsGrokResponse()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var grok = CreateEnabledGrok("Advanced answer");
        var sut = CreateService(db, Mock.Of<IOllamaChatClientFactory>(), grok);

        var result = await sut.AskAdvancedAsync(new AskAdvancedRequest("Question?", "Deadline context"));

        result.UsedGrok.Should().BeTrue();
        result.Answer.Should().Be("Advanced answer");
    }

    [Fact]
    public async Task GetStatusAsync_ReportsOllamaAndGrokState()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var ollama = new Mock<IOllamaChatClientFactory>();
        ollama.Setup(o => o.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        ollama.SetupGet(o => o.ChatModel).Returns("llama3.2:3b");

        var grok = CreateEnabledGrok("ok");
        var sut = CreateService(db, ollama.Object, grok);

        var status = await sut.GetStatusAsync();
        status.OllamaAvailable.Should().BeTrue();
        status.OllamaModel.Should().Be("llama3.2:3b");
        status.GrokEnabled.Should().BeTrue();
    }

    private static HybridAiService CreateService(
        TikrDbContext db,
        IOllamaChatClientFactory ollama,
        GrokService grok) =>
        new(db, ollama, grok, NullLogger<HybridAiService>.Instance);

    private static IOllamaChatClientFactory CreateOllamaFactory(string responseText)
    {
        var factory = new Mock<IOllamaChatClientFactory>();
        factory.Setup(f => f.CreateChatClient()).Returns(new StubChatClient(responseText));
        factory.SetupGet(f => f.ChatModel).Returns("test-model");
        return factory.Object;
    }

    private static GrokService CreateEnabledGrok(string response) =>
        TestGrokServiceFactory.Create(
            new Dictionary<string, string?>
            {
                ["USE_GROK"] = "true",
                ["GROK_API_KEY"] = "xai-key"
            },
            new DelegatingHandlerStub(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(
                    $$"""
                    { "choices": [ { "message": { "role": "assistant", "content": "{{response}}" } } ] }
                    """,
                    System.Text.Encoding.UTF8,
                    "application/json")
            }));

    private static Requirement Requirement(string title, DateOnly dueDate) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        DueDate = dueDate,
        Recurrence = RecurrenceType.Annual,
        Category = RequirementCategory.Compliance,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private sealed class DelegatingHandlerStub(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
