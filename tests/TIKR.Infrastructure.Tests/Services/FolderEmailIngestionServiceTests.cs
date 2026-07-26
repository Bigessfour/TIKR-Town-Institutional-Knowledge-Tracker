using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TIKR.Infrastructure.Data;
using TIKR.Infrastructure.Services;
using TIKR.Infrastructure.Tests.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Tests.Services;

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
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TIKR_EMAIL_INBOX_PATH"] = inbox,
                    ["FILE_STORAGE_PATH"] = storage
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton(db);
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddSingleton<IFileStorageService, LocalFileStorageService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddSingleton<ICurrentUserService>(new StubCurrentUser("email-ingest@town.gov"));
            var provider = services.BuildServiceProvider();

            var sut = new FolderEmailIngestionService(
                provider.GetRequiredService<IServiceScopeFactory>(),
                config,
                NullLogger<FolderEmailIngestionService>.Instance);

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
            try { Directory.Delete(inbox, recursive: true); } catch { /* ignore */ }
            try { Directory.Delete(storage, recursive: true); } catch { /* ignore */ }
        }
    }

    private sealed class StubCurrentUser(string userId) : ICurrentUserService
    {
        public string? UserId { get; } = userId;
        public bool IsAuthenticated => true;
    }
}
