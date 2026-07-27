using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TIKR.Infrastructure.Services;
using TIKR.Infrastructure.Tests.Helpers;
using TIKR.Shared.Entities;
using TIKR.Shared.Interfaces;
using TIKR.Shared.TestFixtures;

namespace TIKR.Infrastructure.Tests.Services;

[Trait("Category", TestCategories.FullyTested)]
public class DocumentServiceUpdateMetadataTests
{
    [Fact]
    public async Task UpdateMetadataAsync_RenamesFileAndMovesFolder()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            FileName = "old.pdf",
            StoragePath = "p/old.pdf",
            SuggestedFolder = "Inbox",
            FileSizeBytes = 10,
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Documents.Add(doc);
        await db.SaveChangesAsync();

        var audit = new Mock<IAuditService>();
        var user = Mock.Of<ICurrentUserService>(u => u.UserId == "clerk");
        var sut = new DocumentService(db, NullLogger<DocumentService>.Instance);

        var updated = await sut.UpdateMetadataAsync(
            doc.Id,
            fileName: "new-name.pdf",
            suggestedFolder: "Finance",
            updateFolder: true,
            audit.Object,
            user);

        updated.FileName.Should().Be("new-name.pdf");
        updated.SuggestedFolder.Should().Be("Finance");
        (await db.Documents.FindAsync(doc.Id))!.FileName.Should().Be("new-name.pdf");
        audit.Verify(a => a.LogAsync(
            "Update",
            nameof(Document),
            doc.Id,
            It.IsAny<string?>(),
            "clerk",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateMetadataAsync_ClearFolder_SetsNull()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            FileName = "x.pdf",
            StoragePath = "p/x.pdf",
            SuggestedFolder = "Finance",
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Documents.Add(doc);
        await db.SaveChangesAsync();

        var sut = new DocumentService(db, NullLogger<DocumentService>.Instance);
        var updated = await sut.UpdateMetadataAsync(
            doc.Id,
            fileName: null,
            suggestedFolder: null,
            updateFolder: true,
            Mock.Of<IAuditService>(),
            Mock.Of<ICurrentUserService>(u => u.UserId == "u"));

        updated.SuggestedFolder.Should().BeNull();
    }

    [Fact]
    public async Task UpdateMetadataAsync_MissingDocument_Throws()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var sut = new DocumentService(db, NullLogger<DocumentService>.Instance);
        var act = () => sut.UpdateMetadataAsync(
            Guid.NewGuid(), "a.pdf", null, false,
            Mock.Of<IAuditService>(), Mock.Of<ICurrentUserService>());
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
