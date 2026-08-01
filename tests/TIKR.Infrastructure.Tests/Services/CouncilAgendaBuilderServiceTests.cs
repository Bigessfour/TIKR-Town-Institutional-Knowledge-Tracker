using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TIKR.Infrastructure;
using TIKR.Infrastructure.Services;
using TIKR.Infrastructure.Tests.Helpers;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;
using TIKR.Shared.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Tests.Services;

public class CouncilAgendaBuilderServiceTests
{
    [Fact]
    public async Task BuildPreviewAsync_IncludesDlgSectionsAndNewBusiness()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        db.Requirements.Add(new Requirement
        {
            Id = Guid.NewGuid(),
            Title = "TABOR notice",
            Description = "Post election notice",
            DueDate = new DateOnly(2026, 8, 9),
            Category = RequirementCategory.Compliance
        });
        await db.SaveChangesAsync();

        var service = new CouncilAgendaBuilderService(db, new StubHybridAi());
        var preview = await service.BuildPreviewAsync(new DateOnly(2026, 8, 10), "TOW");

        preview.Sections.Should().Contain(s => s.SectionKey == "old_business");
        preview.Sections.Single(s => s.SectionKey == "new_business").Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SuggestUnfinishedBusinessAsync_ExtractsFromPriorMinutesDocument()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        db.Documents.Add(new Document
        {
            Id = Guid.NewGuid(),
            FileName = "7 JULY 13 2026 TOW.docx",
            SuggestedFolder = DocumentTagHeuristics.Minutes,
            FullTextContent = "Item on water rates was tabled until the August meeting.",
            ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            FileSizeBytes = 100,
            StoragePath = "test/path",
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new CouncilAgendaBuilderService(db, new StubHybridAi());
        var suggestions = await service.SuggestUnfinishedBusinessAsync(new DateOnly(2026, 8, 10), "TOW");

        suggestions.Should().NotBeEmpty();
        suggestions[0].SourceQuote!.Should().Contain("tabled");
    }

    [Theory]
    [InlineData("7 JULY 13 2026 TOW.docx", 2026, 7, 13, "TOW", true)]
    [InlineData("7 JULY 1 2026 TOW.docx", 2026, 7, 13, "TOW", false)]
    [InlineData("random.pdf", 2026, 7, 13, "TOW", false)]
    public void MatchesPriorMeeting_MatchesExpectedFiles(
        string fileName, int y, int m, int d, string board, bool expected)
    {
        CouncilAgendaBuilderService.MatchesPriorMeeting(fileName, new DateOnly(y, m, d), board)
            .Should().Be(expected);
    }

    [Fact]
    public async Task BuildMinutesPreviewAsync_UsesLinkedAgendaAndDraftRequirement()
    {
        await using var db = await TestDbContextFactory.CreateMigratedAsync();
        await CouncilMeetingSeeder.SeedAsync(db);

        var postAgenda = await db.Requirements.SingleAsync(r =>
            r.Title == "Post Town Council Agenda — August 10, 2026");
        var agendaDoc = new Document
        {
            Id = Guid.NewGuid(),
            FileName = "8 AUGUST 10 2026.docx",
            SuggestedFolder = DocumentTagHeuristics.Agenda,
            FullTextContent = """
                TOWN OF WILEY REGULAR MEETING AGENDA
                August 10, 2026
                Call to Order
                Budget amendment ordinance (first reading)
                Water rate resolution
                """,
            ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            FileSizeBytes = 100,
            StoragePath = "agenda/path",
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Documents.Add(agendaDoc);
        db.RequirementDocuments.Add(new RequirementDocument
        {
            RequirementId = postAgenda.Id,
            DocumentId = agendaDoc.Id,
            LinkedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new CouncilAgendaBuilderService(db, new StubHybridAi());

        var preview = await service.BuildMinutesPreviewAsync(new DateOnly(2026, 8, 10), "TOW");

        preview.ActionedAgendaDocumentId.Should().Be(agendaDoc.Id);
        preview.DraftMinutesRequirementId.Should().NotBeNull();
        preview.AgendaLines.Should().Contain("Budget amendment ordinance (first reading)");
        preview.AgendaLines.Should().NotContain("TABOR notice");
        preview.SuggestedFileName.Should().Contain("TOW");
    }

    private sealed class StubHybridAi : IHybridAiService
    {
        public Task<TagDocumentResponse> TagDocumentAsync(Guid documentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new TagDocumentResponse(documentId, ["minutes"], DocumentTagHeuristics.Minutes));

        public Task<IReadOnlyList<DashboardPriority>> GetDashboardPrioritiesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DashboardPriority>>([]);

        public Task<AskAdvancedResponse> AskAdvancedAsync(AskAdvancedRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AskAdvancedResponse("n/a", false));

        public Task<AiStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new AiStatusResponse(true, "stub", false));

        public Task<SemanticSearchResponse> SemanticSearchDocumentsAsync(SemanticSearchRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SemanticSearchResponse(request.Query, 0, [], EmbeddingAvailable: false));

        public Task<EmbedDocumentResponse> EmbedDocumentAsync(Guid documentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new EmbedDocumentResponse(documentId, true, null));

        public Task<SemanticSearchKnowledgeResponse> SemanticSearchKnowledgeAsync(SemanticSearchRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SemanticSearchKnowledgeResponse(request.Query, 0, [], EmbeddingAvailable: false));

        public Task<EmbedKnowledgeEntryResponse> EmbedKnowledgeEntryAsync(Guid entryId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new EmbedKnowledgeEntryResponse(entryId, true, null));

        public Task<ReindexEmbeddingsResponse> ReindexAllEmbeddingsAsync(string? trigger = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ReindexEmbeddingsResponse(0, 0, 0, 0, [], Trigger: trigger ?? "manual"));

        public Task<CorpusHealthResponse> GetCorpusHealthAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new CorpusHealthResponse(0, 0, 0, 0, 0, 0, 100, 100, []));
    }
}
