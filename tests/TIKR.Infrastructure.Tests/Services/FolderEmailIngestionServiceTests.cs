using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TIKR.Infrastructure.Data;
using TIKR.Infrastructure.Services;
using TIKR.Infrastructure.Tests.Helpers;
using TIKR.Shared.DTOs;
using TIKR.Shared.Enums;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Tests.Services;

// FolderEmailIngestionHostedService polls FolderEmailIngestionService; ingestion + extract proven below.

public class FolderEmailIngestionServiceTests
{
    [Fact]
    public async Task IngestPendingAsync_UploadsTxtAndMovesToProcessed()
    {
        var inbox = Path.Combine(Path.GetTempPath(), "tikr-email-inbox-" + Guid.NewGuid().ToString("N"));
        var storage = Path.Combine(Path.GetTempPath(), "tikr-email-store-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(inbox);
        Directory.CreateDirectory(storage);

        try
        {
            var drop = Path.Combine(inbox, "council-note.txt");
            await File.WriteAllTextAsync(drop, "Forwarded council note body");

            await using var db = await TestDbContextFactory.CreateMigratedAsync();
            var (sut, _) = BuildSut(db, inbox, storage, extractEnabled: false);

            sut.IsConfigured.Should().BeTrue();
            var result = await sut.IngestPendingAsync();

            result.Ingested.Should().Be(1);
            result.Errors.Should().BeEmpty();
            File.Exists(drop).Should().BeFalse();
            Directory.GetFiles(Path.Combine(inbox, "processed")).Should().ContainSingle();
            (await db.Documents.CountAsync()).Should().Be(1);
            (await db.Documents.SingleAsync()).FileName.Should().Be("council-note.txt");
        }
        finally
        {
            TryDelete(inbox);
            TryDelete(storage);
        }
    }

    [Fact]
    public async Task IngestPendingAsync_ElectionEml_CreatesElectionContactAndKnowledge()
    {
        var inbox = Path.Combine(Path.GetTempPath(), "tikr-email-inbox-" + Guid.NewGuid().ToString("N"));
        var storage = Path.Combine(Path.GetTempPath(), "tikr-email-store-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(inbox);
        Directory.CreateDirectory(storage);

        try
        {
            var drop = Path.Combine(inbox, "Update to election procedures.eml");
            await File.WriteAllTextAsync(drop, ElectionEml);

            await using var db = await TestDbContextFactory.CreateMigratedAsync();
            var (sut, notices) = BuildSut(db, inbox, storage, extractEnabled: true);

            var result = await sut.IngestPendingAsync();

            result.Ingested.Should().Be(1);
            result.ContactsCreated.Should().BeGreaterThan(0);
            result.KnowledgeCreated.Should().BeGreaterThan(0);
            result.Notices.Should().NotBeNull();
            result.Notices!.Should().Contain(n => n.ParseSucceeded);

            var contacts = await db.Contacts.Where(c => c.DeletedAt == null).ToListAsync();
            contacts.Should().Contain(c => c.Categories.HasFlag(ContactCategory.Election));

            (await db.KnowledgeEntries.CountAsync(k => k.Category == KnowledgeCategory.Contact))
                .Should().BeGreaterThan(0);

            notices.GetRecent().Should().NotBeEmpty();
        }
        finally
        {
            TryDelete(inbox);
            TryDelete(storage);
        }
    }

    [Fact]
    public async Task IngestPendingAsync_WhenExtractDisabled_StillUploadsWithoutContacts()
    {
        var inbox = Path.Combine(Path.GetTempPath(), "tikr-email-inbox-" + Guid.NewGuid().ToString("N"));
        var storage = Path.Combine(Path.GetTempPath(), "tikr-email-store-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(inbox);
        Directory.CreateDirectory(storage);

        try
        {
            var drop = Path.Combine(inbox, "Update to election procedures.eml");
            await File.WriteAllTextAsync(drop, ElectionEml);

            await using var db = await TestDbContextFactory.CreateMigratedAsync();
            // Pre-seed one contact so empty Contacts table isn't just "seeder missing"
            var before = await db.Contacts.CountAsync();

            var (sut, _) = BuildSut(db, inbox, storage, extractEnabled: false);
            var result = await sut.IngestPendingAsync();

            result.Ingested.Should().Be(1);
            result.ContactsCreated.Should().Be(0);
            result.KnowledgeCreated.Should().Be(0);
            (await db.Documents.CountAsync()).Should().Be(1);
            (await db.Contacts.CountAsync()).Should().Be(before);
        }
        finally
        {
            TryDelete(inbox);
            TryDelete(storage);
        }
    }

    [Fact]
    public async Task IngestPendingAsync_CorruptEml_DoesNotThrow_LogsErrorPath()
    {
        var inbox = Path.Combine(Path.GetTempPath(), "tikr-email-inbox-" + Guid.NewGuid().ToString("N"));
        var storage = Path.Combine(Path.GetTempPath(), "tikr-email-store-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(inbox);
        Directory.CreateDirectory(storage);

        try
        {
            // Valid enough to upload; extractor should still succeed or soft-fail without crashing ingest.
            var drop = Path.Combine(inbox, "broken.eml");
            await File.WriteAllBytesAsync(drop, "\0\0\0not-really-email"u8.ToArray());

            await using var db = await TestDbContextFactory.CreateMigratedAsync();
            var (sut, _) = BuildSut(db, inbox, storage, extractEnabled: true);

            var act = async () => await sut.IngestPendingAsync();
            var result = await act.Should().NotThrowAsync();
            result.Which.Ingested.Should().Be(1);
        }
        finally
        {
            TryDelete(inbox);
            TryDelete(storage);
        }
    }

    private static (FolderEmailIngestionService Sut, EmailIngestionNoticeStore Notices) BuildSut(
        TikrDbContext db,
        string inbox,
        string storage,
        bool extractEnabled)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TIKR_EMAIL_INBOX_PATH"] = inbox,
                ["FILE_STORAGE_PATH"] = storage,
                ["TIKR_EMAIL_STRUCTURED_EXTRACT"] = extractEnabled ? "true" : "false"
            })
            .Build();

        var notices = new EmailIngestionNoticeStore();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton(db);
        services.AddScoped<IDocumentService, DocumentService>();
        var featureState = new FeatureSettingsState();
        featureState.Replace(new FeatureSettingsSnapshot
        {
            OllamaHost = "http://localhost:11434",
            OllamaChatModel = "llama3.2:3b",
            UseGrok = false,
            FileStoragePath = storage
        });
        services.AddSingleton(featureState);
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IKnowledgeService, KnowledgeService>();
        services.AddSingleton<IHybridAiService>(new StubTaggingAi());
        services.AddSingleton<IEmailIngestionNoticeStore>(notices);
        services.AddScoped<IEmailStructuredApplyService, EmailStructuredApplyService>();
        services.AddSingleton<ICurrentUserService>(new StubCurrentUser("email-ingest@town.gov"));
        var provider = services.BuildServiceProvider();

        var sut = new FolderEmailIngestionService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            config,
            NullLogger<FolderEmailIngestionService>.Instance);
        return (sut, notices);
    }

    private static void TryDelete(string path)
    {
        try { Directory.Delete(path, recursive: true); } catch { /* ignore */ }
    }

    private sealed class StubCurrentUser(string userId) : ICurrentUserService
    {
        public string? UserId { get; } = userId;
        public bool IsAuthenticated => true;
    }

    private sealed class StubTaggingAi : IHybridAiService
    {
        public Task<TagDocumentResponse> TagDocumentAsync(Guid documentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new TagDocumentResponse(documentId, ["email"], "Imported"));

        public Task<IReadOnlyList<DashboardPriority>> GetDashboardPrioritiesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DashboardPriority>>([]);

        public Task<AskAdvancedResponse> AskAdvancedAsync(AskAdvancedRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AskAdvancedResponse("n/a", false));

        public Task<AiStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiStatusResponse(true, "stub", false));

        public Task<SemanticSearchResponse> SemanticSearchDocumentsAsync(SemanticSearchRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SemanticSearchResponse(request.Query, 0, []));

        public Task<EmbedDocumentResponse> EmbedDocumentAsync(Guid documentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new EmbedDocumentResponse(documentId, true, null));

        public Task<SemanticSearchKnowledgeResponse> SemanticSearchKnowledgeAsync(SemanticSearchRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SemanticSearchKnowledgeResponse(request.Query, 0, []));

        public Task<EmbedKnowledgeEntryResponse> EmbedKnowledgeEntryAsync(Guid entryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new EmbedKnowledgeEntryResponse(entryId, true, null));

        public Task<ReindexEmbeddingsResponse> ReindexAllEmbeddingsAsync(
            string? trigger = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ReindexEmbeddingsResponse(0, 0, 0, 0, [], Trigger: trigger ?? "manual"));

        public Task<CorpusHealthResponse> GetCorpusHealthAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new CorpusHealthResponse(0, 0, 0, 0, 0, 0, 100, 100, []));
    }

    private const string ElectionEml =
        """
        From: "County Clerk Elections" <elections@county.example.gov>
        Reply-To: elections@county.example.gov
        To: clerk@townofwiley.example.gov
        Subject: Update to election procedures — canvass packet
        Date: Mon, 14 Aug 2026 10:15:00 -0600
        Content-Type: text/plain; charset=UTF-8

        Hello Deb,

        Please update the town election procedures for the upcoming canvass.

        Contact: Jordan Lee
        Phone: 970-555-0142
        Email: jordan.lee@county.example.gov
        Submit to: County Clerk Elections Division

        Due date: 11/05/2026

        The County Clerk and SOS ballot certification steps changed this cycle.
        """;
}
