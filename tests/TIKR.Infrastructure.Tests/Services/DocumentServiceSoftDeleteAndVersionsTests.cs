using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TIKR.Infrastructure.Services;
using TIKR.Infrastructure.Tests.Helpers;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;
using TIKR.Shared.Interfaces;
using TIKR.Shared.TestFixtures;

namespace TIKR.Infrastructure.Tests.Services;

[Trait("Category", TestCategories.FullyTested)]
public class DocumentServiceSoftDeleteAndVersionsTests
{
    [Fact]
    public async Task SoftDelete_SetsDeletedAt_AndRemovesChunks()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var doc = SeedDoc(db, "soft.pdf", "body for soft delete tests enough letters");
        db.EmbeddingChunks.Add(new EmbeddingChunk
        {
            Id = Guid.NewGuid(),
            SourceType = EmbeddingSourceType.Document,
            SourceId = doc.Id,
            ChunkIndex = 0,
            Content = "chunk",
            ContentHash = "abc",
            Embedding = HybridAiService.PackFloats([0.1f, 0.2f]),
            DisplayName = doc.FileName
        });
        await db.SaveChangesAsync();

        var sut = new DocumentService(db, NullLogger<DocumentService>.Instance);
        await sut.SoftDeleteAsync(doc.Id, Mock.Of<IAuditService>(), Mock.Of<ICurrentUserService>(u => u.UserId == "u"));

        var reloaded = await db.Documents.FindAsync(doc.Id);
        reloaded!.DeletedAt.Should().NotBeNull();
        db.EmbeddingChunks.Count(c => c.SourceId == doc.Id).Should().Be(0);
    }

    [Fact]
    public async Task Restore_ClearsDeletedAt()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var doc = SeedDoc(db, "restore.pdf", "content");
        doc.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var sut = new DocumentService(db, NullLogger<DocumentService>.Instance);
        await sut.RestoreAsync(doc.Id, Mock.Of<IAuditService>(), Mock.Of<ICurrentUserService>(u => u.UserId == "u"));

        (await db.Documents.FindAsync(doc.Id))!.DeletedAt.Should().BeNull();
    }

    [Fact]
    public async Task ReplaceContent_KeepsPriorVersionSnapshot()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var storage = new InMemoryFileStorage();
        await using var initial = new MemoryStream("version one body with enough text"u8.ToArray());
        var path = await storage.SaveAsync(initial, "ver.txt");
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            FileName = "ver.txt",
            StoragePath = path,
            FileSizeBytes = 30,
            FullTextContent = "version one body with enough text",
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Documents.Add(doc);
        await db.SaveChangesAsync();

        var sut = new DocumentService(db, NullLogger<DocumentService>.Instance);
        await using var next = new MemoryStream("version two body with enough text"u8.ToArray());
        await sut.ReplaceContentAsync(
            doc.Id, next, "text/plain", next.Length, storage,
            Mock.Of<IAuditService>(), Mock.Of<ICurrentUserService>(u => u.UserId == "u"));

        var versions = await sut.ListVersionsAsync(doc.Id);
        versions.Should().ContainSingle();
        versions[0].VersionNumber.Should().Be(1);
        versions[0].StoragePath.Should().Be(path);
    }

    [Fact]
    public async Task Purge_RemovesDocumentAndVersions()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var storage = new InMemoryFileStorage();
        await using var stream = new MemoryStream("purge me body text content here"u8.ToArray());
        var path = await storage.SaveAsync(stream, "purge.txt");
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            FileName = "purge.txt",
            StoragePath = path,
            DeletedAt = DateTime.UtcNow,
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Documents.Add(doc);
        db.DocumentVersions.Add(new DocumentVersion
        {
            Id = Guid.NewGuid(),
            DocumentId = doc.Id,
            VersionNumber = 1,
            FileName = "purge.txt",
            StoragePath = path + ".v1",
            FileSizeBytes = 10,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new DocumentService(db, NullLogger<DocumentService>.Instance);
        await sut.PurgeAsync(doc.Id, storage, Mock.Of<IAuditService>(), Mock.Of<ICurrentUserService>(u => u.UserId == "u"));

        (await db.Documents.FindAsync(doc.Id)).Should().BeNull();
        db.DocumentVersions.Count(v => v.DocumentId == doc.Id).Should().Be(0);
    }

    private static Document SeedDoc(TIKR.Infrastructure.Data.TikrDbContext db, string name, string text)
    {
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            FileName = name,
            StoragePath = "p/" + name,
            FullTextContent = text,
            FileSizeBytes = text.Length,
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Documents.Add(doc);
        return doc;
    }

    private sealed class InMemoryFileStorage : IFileStorageService
    {
        private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

        public Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
        {
            using var ms = new MemoryStream();
            content.CopyTo(ms);
            var path = $"mem/{Guid.NewGuid():N}_{Path.GetFileName(fileName)}";
            _files[path] = ms.ToArray();
            return Task.FromResult(path);
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream(_files[storagePath]));

        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            _files.Remove(storagePath);
            return Task.CompletedTask;
        }

        public string GetFullPath(string storagePath) => storagePath;
    }
}
