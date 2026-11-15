using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure.Data;
using TIKR.Infrastructure.Services;
using TIKR.Infrastructure.Tests.Helpers;
using TIKR.Shared.Entities;
using TIKR.Shared.TestFixtures;

namespace TIKR.Infrastructure.Tests.Services;

[Trait("Category", TestCategories.FullyTested)]
public class DocumentFolderCatalogTests
{
    [Fact]
    public async Task AcceptAsync_CreatesFolderAndFilesTheGroup()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var first = Pending(db, "ELECTION INFORMATION");
        var second = Pending(db, "election information");
        Pending(db, "Grants");
        await db.SaveChangesAsync();

        var result = await new DocumentFolderCatalog(db).AcceptAsync("ELECTION INFORMATION");

        result.Name.Should().Be("ELECTION INFORMATION");
        result.Updated.Should().Be(2);
        var filed = await db.Documents.Where(d => d.Id == first.Id || d.Id == second.Id).ToListAsync();
        filed.Should().OnlyContain(d =>
            d.SuggestedFolder == "ELECTION INFORMATION" && d.ProposedFolder == null);
        var directory = await new DocumentFolderCatalog(db).GetAsync();
        directory.MatchAllowed("election information").Should().Be("ELECTION INFORMATION");
    }

    [Fact]
    public async Task DeclineAsync_KeepsFilesInGeneralAndRemembersTheName()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var document = Pending(db, "Elections");
        await db.SaveChangesAsync();
        var catalog = new DocumentFolderCatalog(db);

        var result = await catalog.DeclineAsync("Elections");

        result.Updated.Should().Be(1);
        var updated = await db.Documents.SingleAsync(d => d.Id == document.Id);
        updated.SuggestedFolder.Should().Be(DocumentTagHeuristics.General);
        updated.ProposedFolder.Should().BeNull();
        (await catalog.GetAsync()).IsDeclined("elections").Should().BeTrue();
    }

    [Fact]
    public void CanonicalOrNull_KeepsBudgetLabelAndRejectsPaths()
    {
        FolderNamePolicy.CanonicalOrNull("Budget / Finance").Should().Be("Budget / Finance");
        FolderNamePolicy.CanonicalOrNull("ELECTION/INFORMATION").Should().BeNull();
    }

    [Fact]
    public async Task DeclineAsync_RejectsBuiltInFolder()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        var act = async () => await new DocumentFolderCatalog(db).DeclineAsync(DocumentTagHeuristics.Minutes);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    private static Document Pending(TikrDbContext db, string proposed)
    {
        var document = new Document
        {
            Id = Guid.NewGuid(),
            FileName = $"{proposed}.pdf",
            StoragePath = $"2026/{Guid.NewGuid():N}.pdf",
            SuggestedFolder = DocumentTagHeuristics.General,
            ProposedFolder = proposed,
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Documents.Add(document);
        return document;
    }
}
