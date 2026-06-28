using FluentAssertions;
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
        var storage = new InMemoryFileStorage();
        var sut = new DocumentAgentService(storage);
        await using var content = new MemoryStream("sample ordinance text"u8.ToArray());

        var result = await sut.ProcessUploadAsync(content, "budget-report.pdf");

        result.ProcessedLocally.Should().BeTrue();
        result.StoragePath.Should().NotBeNullOrWhiteSpace();
        result.SuggestedCategory.Should().Be(RequirementCategory.Budget);
        result.TablesExtractedCount.Should().Be(3);
        storage.SavedFileNames.Should().ContainSingle().Which.Should().Be("budget-report.pdf");
    }

    private sealed class InMemoryFileStorage : Shared.Interfaces.IFileStorageService
    {
        public List<string> SavedFileNames { get; } = [];

        public Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
        {
            SavedFileNames.Add(fileName);
            return Task.FromResult($"agent/{fileName}");
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream());

        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public string GetFullPath(string storagePath) => storagePath;
    }
}
