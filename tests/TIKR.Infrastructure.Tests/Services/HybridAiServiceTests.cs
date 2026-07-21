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
using TIKR.Shared.Interfaces;
using TIKR.Shared.TestFixtures;

namespace TIKR.Infrastructure.Tests.Services;

[Trait("Category", TestCategories.FullyTested)]
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
    public async Task TagDocumentAsync_UsesLowTemperature()
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

        var chat = new StubChatClient("""{"tags":["budget"],"suggestedFolder":"Budget / Finance"}""");
        var ollama = CreateOllamaFactory(responseText: "", chatClient: chat);
        var sut = CreateService(db, ollama, DisabledGrok);

        await sut.TagDocumentAsync(document.Id);

        chat.LastOptions.Should().NotBeNull();
        chat.LastOptions!.Temperature.Should().Be(DocumentTagPromptBuilder.TaggingTemperature);
    }

    [Fact]
    public async Task AskAdvancedAsync_DoesNotForceTaggingTemperature()
    {
        var chat = new StubChatClient("local answer");
        var ollama = CreateOllamaFactory(responseText: "", chatClient: chat);
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = CreateService(db, ollama, DisabledGrok);

        var result = await sut.AskAdvancedAsync(new AskAdvancedRequest("hello", null));

        result.Answer.Should().Be("local answer");
        chat.LastOptions.Should().BeNull();
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
    public async Task TagDocumentAsync_ResumeFilename_UsesHeuristicsWhenOllamaEmpty()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = "Stephen_McKitrick_Resume.pdf",
            StoragePath = "2026/01/Stephen_McKitrick_Resume.pdf",
            ContentType = "application/pdf",
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Documents.Add(document);
        await db.SaveChangesAsync();

        var sut = CreateService(db, CreateOllamaFactory(""), DisabledGrok);
        var result = await sut.TagDocumentAsync(document.Id);

        result.SuggestedFolder.Should().Be(DocumentTagHeuristics.PersonnelHr);
        result.Tags.Should().BeEquivalentTo(["resume", "personnel"]);
    }

    [Fact]
    public async Task TagDocumentAsync_BackfillsFullTextFromExtractor()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = "minutes.pdf",
            StoragePath = "2026/01/minutes.pdf",
            ContentType = "application/pdf",
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Documents.Add(document);
        await db.SaveChangesAsync();

        var storage = new Mock<IFileStorageService>();
        storage.Setup(s => s.OpenReadAsync(document.StoragePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream("%PDF-fake"u8.ToArray()));

        var extraction = new Mock<IDocumentAgentExtractionBackend>();
        extraction.Setup(e => e.ExtractAsync(It.IsAny<Stream>(), document.FileName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentExtractionResult(
                "Town council minutes for July meeting. Attendance recorded.",
                TablesExtractedCount: 0,
                UsedSyncfusionTools: true));

        var ollama = CreateOllamaFactory(
            """{"tags":["minutes"],"suggestedFolder":"Minutes"}""",
            _ => new[] { 0f, 1f, 0f, 0f });
        var sut = CreateService(db, ollama, DisabledGrok, storage.Object, extraction.Object);

        await sut.TagDocumentAsync(document.Id);

        var updated = await db.Documents.FindAsync(document.Id);
        updated!.FullTextContent.Should().Contain("Town council minutes");
        updated.Embedding.Should().NotBeNull();
    }

    [Fact]
    public async Task TagDocumentAsync_DoesNotPersistStubExtractorPlaceholder()
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

        var storage = new Mock<IFileStorageService>();
        storage.Setup(s => s.OpenReadAsync(document.StoragePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new MemoryStream("%PDF-fake"u8.ToArray()));

        var sut = CreateService(
            db,
            CreateOllamaFactory("""{"tags":["x"],"suggestedFolder":"General"}"""),
            DisabledGrok,
            storage.Object,
            new StubDocumentAgentExtractionBackend());

        await sut.TagDocumentAsync(document.Id);

        var updated = await db.Documents.FindAsync(document.Id);
        updated!.FullTextContent.Should().BeNull();
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
    public async Task AskAdvancedAsync_FallsBackToOllamaWhenGrokDisabled()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var ollama = CreateOllamaFactory("Local fallback answer");
        var sut = CreateService(db, ollama, DisabledGrok);

        var result = await sut.AskAdvancedAsync(new AskAdvancedRequest("hello", null));

        result.UsedGrok.Should().BeFalse();
        result.Answer.Should().Be("Local fallback answer");
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

    [Fact]
    public async Task GetStatusAsync_ReportsOllamaUnavailable()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var ollama = new Mock<IOllamaChatClientFactory>();
        ollama.Setup(o => o.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        ollama.SetupGet(o => o.ChatModel).Returns("llama3.2:3b");

        var sut = CreateService(db, ollama.Object, DisabledGrok);
        var status = await sut.GetStatusAsync();
        status.OllamaAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task GetDashboardPrioritiesAsync_ExcludesCompletedRequirements()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Requirements.Add(Requirement("Done item", today.AddDays(3), isCompleted: true));
        db.Requirements.Add(Requirement("Active item", today.AddDays(3)));
        await db.SaveChangesAsync();

        var sut = CreateService(db, Mock.Of<IOllamaChatClientFactory>(), DisabledGrok);
        var priorities = await sut.GetDashboardPrioritiesAsync();

        priorities.Should().Contain(p => p.Title == "Active item");
        priorities.Should().NotContain(p => p.Title == "Done item");
    }

    [Fact]
    public async Task GetDashboardPrioritiesAsync_ExcludesRequirementsOlderThan30Days()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Requirements.Add(Requirement("Ancient overdue", today.AddDays(-45)));
        db.Requirements.Add(Requirement("Recent overdue", today.AddDays(-5)));
        await db.SaveChangesAsync();

        var sut = CreateService(db, Mock.Of<IOllamaChatClientFactory>(), DisabledGrok);
        var priorities = await sut.GetDashboardPrioritiesAsync();

        priorities.Should().Contain(p => p.Title == "Recent overdue");
        priorities.Should().NotContain(p => p.Title == "Ancient overdue");
    }

    [Fact]
    public async Task GetDashboardPrioritiesAsync_AssignsMediumPriority()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        db.Requirements.Add(Requirement("Medium window", today.AddDays(20)));
        await db.SaveChangesAsync();

        var sut = CreateService(db, Mock.Of<IOllamaChatClientFactory>(), DisabledGrok);
        var priorities = await sut.GetDashboardPrioritiesAsync();

        priorities.Should().ContainSingle(p => p.Title == "Medium window" && p.Priority == "Medium");
    }

    [Fact]
    public async Task GetDashboardPrioritiesAsync_CapsAtTenItems()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var i = 0; i < 12; i++)
            db.Requirements.Add(Requirement($"Item {i:D2}", today.AddDays(i + 1)));
        await db.SaveChangesAsync();

        var sut = CreateService(db, Mock.Of<IOllamaChatClientFactory>(), DisabledGrok);
        var priorities = await sut.GetDashboardPrioritiesAsync();

        priorities.Should().HaveCount(10);
    }

    [Fact]
    public async Task AskAdvancedAsync_ReturnsFallbackWhenGrokReturnsNull()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var grok = TestGrokServiceFactory.Create(
            new Dictionary<string, string?> { ["USE_GROK"] = "true", ["GROK_API_KEY"] = "unit-test-grok-key-not-a-real-credential" },
            new DelegatingHandlerStub(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("""{ "choices": [] }""", System.Text.Encoding.UTF8, "application/json")
            }));
        var ollama = CreateOllamaFactory("Ollama after Grok miss");
        var sut = CreateService(db, ollama, grok);

        var result = await sut.AskAdvancedAsync(new AskAdvancedRequest("Question?", null));
        result.UsedGrok.Should().BeFalse();
        result.Answer.Should().Be("Ollama after Grok miss");
    }

    [Fact]
    public async Task AskAdvancedAsync_UsesPromptOnlyWhenContextMissing()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        string? capturedBody = null;
        var grok = TestGrokServiceFactory.Create(
            new Dictionary<string, string?> { ["USE_GROK"] = "true", ["GROK_API_KEY"] = "unit-test-grok-key-not-a-real-credential" },
            new DelegatingHandlerStub(req =>
            {
                capturedBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{ "choices": [ { "message": { "content": "ok" } } ] }""",
                        System.Text.Encoding.UTF8,
                        "application/json")
                };
            }));
        var sut = CreateService(db, Mock.Of<IOllamaChatClientFactory>(), grok);

        await sut.AskAdvancedAsync(new AskAdvancedRequest("Plain question", null));

        capturedBody.Should().Contain("Plain question");
        capturedBody.Should().NotContain("Context:");
    }

    [Fact]
    public async Task TagDocumentAsync_PersistsEmbeddingWhenEmbedderSucceeds()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = "embed-me.pdf",
            StoragePath = "2026/01/embed-me.pdf",
            ContentType = "application/pdf",
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Documents.Add(document);
        await db.SaveChangesAsync();

        var ollama = CreateOllamaFactory(
            """{"tags":["x"],"suggestedFolder":"F"}""",
            _ => new[] { 1f, 0f, 0f, 0f });
        var sut = CreateService(db, ollama, DisabledGrok);

        await sut.TagDocumentAsync(document.Id);

        var updated = await db.Documents.FindAsync(document.Id);
        updated!.Embedding.Should().NotBeNull();
    }

    [Fact]
    public void BuildEmbeddingText_IncludesFullTextBody()
    {
        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = "resume.pdf",
            StoragePath = "x",
            SuggestedFolder = DocumentTagHeuristics.PersonnelHr,
            AiTags = """["resume","personnel"]""",
            FullTextContent = "Software engineer with municipal experience.",
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var text = HybridAiService.BuildEmbeddingText(document);
        text.Should().Contain("resume.pdf");
        text.Should().Contain(DocumentTagHeuristics.PersonnelHr);
        text.Should().Contain("Software engineer with municipal experience.");
    }

    private static HybridAiService CreateService(
        TikrDbContext db,
        IOllamaChatClientFactory ollama,
        GrokService grok,
        IFileStorageService? storage = null,
        IDocumentAgentExtractionBackend? extraction = null) =>
        new(
            db,
            ollama,
            grok,
            storage ?? Mock.Of<IFileStorageService>(),
            extraction ?? Mock.Of<IDocumentAgentExtractionBackend>(),
            NullLogger<HybridAiService>.Instance);

    private static IOllamaChatClientFactory CreateOllamaFactory(
        string responseText,
        Func<string, float[]?>? embedder = null,
        StubChatClient? chatClient = null)
    {
        var factory = new Mock<IOllamaChatClientFactory>();
        factory.Setup(f => f.CreateChatClient()).Returns(chatClient ?? new StubChatClient(responseText));
        factory.SetupGet(f => f.ChatModel).Returns("test-model");
        factory.Setup(f => f.CreateEmbeddingGenerator(It.IsAny<string>()))
            .Returns(new StubEmbeddingGenerator(embedder ?? (_ => null)));
        return factory.Object;
    }

    private static GrokService CreateEnabledGrok(string response) =>
        TestGrokServiceFactory.Create(
            new Dictionary<string, string?>
            {
                ["USE_GROK"] = "true",
                ["GROK_API_KEY"] = "unit-test-grok-key-not-a-real-credential"
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

    private static Requirement Requirement(string title, DateOnly dueDate, bool isCompleted = false) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        DueDate = dueDate,
        Recurrence = RecurrenceType.Annual,
        Category = RequirementCategory.Compliance,
        IsCompleted = isCompleted,
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
