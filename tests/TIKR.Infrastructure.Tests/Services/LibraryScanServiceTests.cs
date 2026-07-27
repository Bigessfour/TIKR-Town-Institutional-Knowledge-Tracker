using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TIKR.Infrastructure.Data;
using TIKR.Infrastructure.Services;
using TIKR.Infrastructure.Tests.Helpers;
using TIKR.Shared.DTOs;
using TIKR.Shared.Interfaces;
// DbUpdateException used by unique-path unit checks

namespace TIKR.Infrastructure.Tests.Services;

public class LibraryScanServiceTests
{
    [Fact]
    public void IsAllowedExtension_AcceptsClerkDocumentTypes()
    {
        LibraryScanService.IsAllowedExtension("memo.pdf").Should().BeTrue();
        LibraryScanService.IsAllowedExtension("notes.TXT").Should().BeTrue();
        LibraryScanService.IsAllowedExtension("scan.tif").Should().BeFalse();
        LibraryScanService.IsAllowedExtension("photo.jpg").Should().BeFalse();
        LibraryScanService.IsAllowedExtension("archive.zip").Should().BeFalse();
    }

    [Fact]
    public async Task ScanAsync_CopiesTxtLeavesSource_AndSkipsOnRescan()
    {
        var library = Path.Combine(Path.GetTempPath(), "tikr-library-" + Guid.NewGuid().ToString("N"));
        var storage = Path.Combine(Path.GetTempPath(), "tikr-library-store-" + Guid.NewGuid().ToString("N"));
        var nested = Path.Combine(library, "ordinances");
        Directory.CreateDirectory(nested);
        Directory.CreateDirectory(storage);

        try
        {
            var source = Path.Combine(nested, "water-rate.txt");
            await File.WriteAllTextAsync(source, "Distinctive phrase: aqueduct levy schedule Q3");

            await using var db = await TestDbContextFactory.CreateMigratedAsync();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TIKR_LIBRARY_SCAN_PATH"] = library,
                    ["FILE_STORAGE_PATH"] = storage
                })
                .Build();

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
            services.AddSingleton<ICurrentUserService>(new StubCurrentUser("library-scan@town.gov"));
            services.AddSingleton<IHybridAiService>(new StubTaggingAi());
            var provider = services.BuildServiceProvider();

            var sut = new LibraryScanService(
                provider.GetRequiredService<IServiceScopeFactory>(),
                config,
                NullLogger<LibraryScanService>.Instance);

            sut.IsConfigured.Should().BeTrue();
            var first = await sut.ScanAsync();

            first.Imported.Should().Be(1);
            first.Failed.Should().Be(0);
            File.Exists(source).Should().BeTrue("source NAS files must not be moved or deleted");
            (await db.Documents.CountAsync()).Should().Be(1);
            (await db.LibraryImportRecords.CountAsync()).Should().Be(1);
            (await db.LibraryImportRecords.SingleAsync()).RelativePath.Should().Contain("water-rate.txt");

            var second = await sut.ScanAsync();
            second.Imported.Should().Be(0);
            second.Skipped.Should().BeGreaterThan(0);
            (await db.Documents.CountAsync()).Should().Be(1);

            var status = sut.GetStatus();
            status.Configured.Should().BeTrue();
            status.LastResult.Should().NotBeNull();
            status.LibraryPath.Should().Be(library);
        }
        finally
        {
            try { Directory.Delete(library, recursive: true); } catch { /* ignore */ }
            try { Directory.Delete(storage, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public async Task ScanAsync_ReturnsErrorWhenPathMissing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TIKR_LIBRARY_SCAN_PATH"] = Path.Combine(Path.GetTempPath(), "tikr-missing-" + Guid.NewGuid().ToString("N"))
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        var provider = services.BuildServiceProvider();

        var sut = new LibraryScanService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            config,
            NullLogger<LibraryScanService>.Instance);

        var result = await sut.ScanAsync();
        result.Imported.Should().Be(0);
        result.Errors.Should().Contain(e => e.Contains("does not exist", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ScanAsync_SerializesConcurrentCalls_NoDuplicateDocuments()
    {
        var library = Path.Combine(Path.GetTempPath(), "tikr-library-conc-" + Guid.NewGuid().ToString("N"));
        var storage = Path.Combine(Path.GetTempPath(), "tikr-library-store-conc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(library);
        Directory.CreateDirectory(storage);

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(library, "memo.txt"),
                "Concurrent library scan must not create duplicate documents.");

            await using var db = await TestDbContextFactory.CreateMigratedAsync();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TIKR_LIBRARY_SCAN_PATH"] = library,
                    ["FILE_STORAGE_PATH"] = storage
                })
                .Build();

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
            services.AddSingleton<ICurrentUserService>(new StubCurrentUser("library-scan@town.gov"));
            services.AddSingleton<IHybridAiService>(new StubTaggingAi());
            var provider = services.BuildServiceProvider();

            var sut = new LibraryScanService(
                provider.GetRequiredService<IServiceScopeFactory>(),
                config,
                NullLogger<LibraryScanService>.Instance);

            var t1 = sut.ScanAsync();
            var t2 = sut.ScanAsync();
            var results = await Task.WhenAll(t1, t2);

            results.Sum(r => r.Imported).Should().Be(1, "only one import across concurrent scans");
            results.Sum(r => r.Failed).Should().Be(0);
            (await db.Documents.CountAsync()).Should().Be(1);
            (await db.LibraryImportRecords.CountAsync()).Should().Be(1);
            sut.ScanInProgress.Should().BeFalse();
            sut.GetStatus().ScanInProgress.Should().BeFalse();
        }
        finally
        {
            try { Directory.Delete(library, recursive: true); } catch { /* ignore */ }
            try { Directory.Delete(storage, recursive: true); } catch { /* ignore */ }
        }
    }

    [Theory]
    [InlineData("UNIQUE constraint failed: LibraryImportRecords.RelativePath", true)]
    [InlineData("Some other database error", false)]
    public void IsUniqueRelativePathViolation_DetectsSqliteUnique(string message, bool expected)
    {
        var inner = new Exception(message);
        var ex = new DbUpdateException("save failed", inner);
        LibraryScanService.IsUniqueRelativePathViolation(ex).Should().Be(expected);
    }

    private sealed class StubCurrentUser(string userId) : ICurrentUserService
    {
        public string? UserId { get; } = userId;
        public bool IsAuthenticated => true;
    }

    private sealed class StubTaggingAi : IHybridAiService
    {
        public Task<TagDocumentResponse> TagDocumentAsync(Guid documentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new TagDocumentResponse(documentId, ["library"], "Imported"));

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
}
