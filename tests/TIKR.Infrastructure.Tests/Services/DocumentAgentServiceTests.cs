using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TIKR.Infrastructure.Services;
using TIKR.Shared.Enums;

namespace TIKR.Infrastructure.Tests.Services;

public class DocumentAgentServiceTests
{
    [Theory]
    [InlineData("annual-budget.pdf", "annual budget", RequirementCategory.Budget, 3)]
    [InlineData("election_canvass.docx", "election canvass", RequirementCategory.Election, 1)]
    [InlineData("notes.txt", "notes", RequirementCategory.Custom, 1)]
    public void StubInference_MapsFilenameToCategory(string fileName, string expectedTitleFragment, RequirementCategory category, int tables)
    {
        DocumentAgentService.DeriveTitle(fileName).ToLowerInvariant()
            .Should().Contain(expectedTitleFragment.ToLowerInvariant());
        DocumentAgentService.InferCategory(DocumentAgentService.DeriveTitle(fileName)).Should().Be(category);
        DocumentAgentService.InferTableCount(fileName).Should().Be(tables);
    }

    [Fact]
    public async Task ProcessUploadAsync_SavesToStorageAndReturnsLocalResult()
    {
        var sut = CreateService(new ConfigurationBuilder().Build());
        await using var content = new MemoryStream("sample ordinance text"u8.ToArray());

        var result = await sut.ProcessUploadAsync(content, "budget-report.pdf");

        result.ProcessedLocally.Should().BeTrue();
        result.StoragePath.Should().StartWith("agent-scans/");
        result.SuggestedCategory.Should().Be(RequirementCategory.Budget);
        result.TablesExtractedCount.Should().Be(3);
    }

    [Fact]
    public async Task ProcessUploadAsync_ExtractsPlainTextFromTxtUpload()
    {
        var sut = CreateService(new ConfigurationBuilder().Build());
        await using var content = new MemoryStream("Colorado periodic report due Q1"u8.ToArray());

        var result = await sut.ProcessUploadAsync(content, "periodic-report.txt");

        result.ExtractedText.Should().Contain("Colorado periodic report");
        result.TablesExtractedCount.Should().Be(1);
    }

    [Fact]
    public async Task ProcessUploadAsync_AcceptsGenerationServiceForArchivePath()
    {
        // Proof for new trackable functions in agent archive extension (10C-G):
        // - DocumentAgentService.ProcessUploadAsync updated for dual storage + archive call
        // - SyncfusionDocumentGenerationService.CreateAgentArchivePdfAsync integration point
        // The real Syncfusion path sets UsedSyncfusionTools=true and exercises the generator.
        var fileStorage = new InMemoryFileStorage();
        var agentStorage = new NasAgentDocumentStorage(fileStorage, new ConfigurationBuilder().Build());
        var backend = new StubDocumentAgentExtractionBackend(); // normal stub returns false; this proves ctor + optional param
        var fakeGenerator = new FakeArchiveGenerator();
        var sut = new DocumentAgentService(agentStorage, backend, fakeGenerator);

        await using var content = new MemoryStream("sample"u8.ToArray());
        var result = await sut.ProcessUploadAsync(content, "test.pdf");

        // With stub (false flag) we still get original path; generator is accepted without crash
        result.OriginalStoragePath.Should().StartWith("agent-scans/");
        result.ProcessedLocally.Should().BeTrue();
        // When flag=true + generator, processedPath is populated (see ProcessUploadAsync_WithTrueSyncfusionBackendAndGenerator_SetsDualStoragePaths)
    }

    [Fact]
    public async Task ProcessUploadAsync_WithTrueSyncfusionBackendAndGenerator_SetsDualStoragePaths()
    {
        // Proves real dual-storage behavior exercised by DocumentAgentService when the extraction backend
        // signals UsedSyncfusionTools=true and a generator is provided (matches licensed agent-scan path).
        var fileStorage = new InMemoryFileStorage();
        var agentStorage = new NasAgentDocumentStorage(fileStorage, new ConfigurationBuilder().Build());
        var trueBackend = new TrueSyncfusionStubBackend();
        var fakeGenerator = new FakeArchiveGenerator();
        var sut = new DocumentAgentService(agentStorage, trueBackend, fakeGenerator);

        await using var content = new MemoryStream("sample ordinance text"u8.ToArray());
        var result = await sut.ProcessUploadAsync(content, "budget-report.pdf");

        result.UsedSyncfusionTools.Should().BeTrue();
        result.OriginalStoragePath.Should().StartWith("agent-scans/");
        result.ProcessedStoragePath.Should().NotBeNullOrWhiteSpace();
        result.ProcessedStoragePath.Should().Contain(".ai-archive.pdf");
        result.ProcessedStoragePath.Should().NotBe(result.OriginalStoragePath);
        result.StoragePath.Should().Be(result.ProcessedStoragePath); // prefers processed when available
        fileStorage.SavedFileNames.Should().ContainMatch("agent-scans/*budget-report.pdf*");
        fileStorage.SavedFileNames.Should().ContainMatch("agent-scans/*ai-archive.pdf*");
    }

    private sealed class FakeArchiveGenerator : Shared.Interfaces.IDocumentGenerationService
    {
        // Minimal fake proving the CreateAgentArchivePdfAsync signature and call site
        public Task<Shared.DTOs.GeneratedDocumentResult> CreateAgentArchivePdfAsync(Stream content, string fileName, DateTime processedDate, CancellationToken cancellationToken = default)
            => Task.FromResult(new Shared.DTOs.GeneratedDocumentResult(new byte[] { 0x25, 0x50, 0x44, 0x46 }, "test.ai-archive.pdf", "application/pdf"));

        public Task<Shared.DTOs.GeneratedDocumentResult> GenerateCouncilAgendaPdfAsync(Shared.DTOs.CouncilAgendaRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Shared.DTOs.GeneratedDocumentResult> GenerateMeetingMinutesDocxAsync(Shared.DTOs.MeetingMinutesRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Shared.DTOs.GeneratedDocumentResult> GenerateClerkMemoDocxAsync(Shared.DTOs.ClerkMemoRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Shared.DTOs.GeneratedDocumentResult> GenerateComplianceReportXlsxAsync(Shared.DTOs.ComplianceReportRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Shared.DTOs.GeneratedDocumentResult> ConvertWordToPdfAsync(Stream wordContent, string fileName, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Shared.DTOs.GeneratedDocumentResult> ConvertExcelToPdfAsync(Stream excelContent, string fileName, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Shared.DTOs.GeneratedDocumentResult> ConvertImageToPdfAsync(Stream imageContent, string fileName, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Shared.DTOs.CouncilPacketGeneratedFiles> GenerateCouncilPacketAsync(Shared.DTOs.CreateCouncilPacketRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Shared.DTOs.GeneratedDocumentResult> GenerateHandoverPackagePdfAsync(Shared.DTOs.HandoverPackageRequest request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    /// <summary>
    /// Minimal backend that signals Syncfusion tools were used so the archive/dual-storage branch executes.
    /// </summary>
    private sealed class TrueSyncfusionStubBackend : Shared.Interfaces.IDocumentAgentExtractionBackend
    {
        public Task<Shared.Interfaces.AgentExtractionResult> ExtractAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
            => Task.FromResult(new Shared.Interfaces.AgentExtractionResult("extracted", 1, UsedSyncfusionTools: true));
    }

    private static DocumentAgentService CreateService(IConfiguration configuration)
    {
        var fileStorage = new InMemoryFileStorage();
        var agentStorage = new NasAgentDocumentStorage(fileStorage, configuration);
        var backend = new StubDocumentAgentExtractionBackend();
        // Pass null for documentGenerationService (archive path only exercised when UsedSyncfusionTools=true + generator provided)
        return new DocumentAgentService(agentStorage, backend, documentGenerationService: null);
    }

    private sealed class InMemoryFileStorage : Shared.Interfaces.IFileStorageService
    {
        public List<string> SavedFileNames { get; } = [];

        public Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
        {
            SavedFileNames.Add(fileName);
            return Task.FromResult(fileName);
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream());

        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public string GetFullPath(string storagePath) => storagePath;
    }
}
