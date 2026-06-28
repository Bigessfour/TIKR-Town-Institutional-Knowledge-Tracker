using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TIKR.Infrastructure.Services;
using TIKR.Infrastructure.Tests.Helpers;

namespace TIKR.Infrastructure.Tests.Services;

public class SyncfusionDocumentAgentToolRegistryTests
{
    [Fact]
    public void GetFunctions_IncludesClerkExtractionTools()
    {
        var storage = new NasSyncfusionDocumentStorage(new InMemoryFileStorage());
        var sut = new SyncfusionDocumentAgentToolRegistry(storage);

        var names = sut.GetFunctions().Select(f => f.Name).ToHashSet(StringComparer.Ordinal);
        names.Should().Contain("PDF_ExtractText");
        names.Should().Contain("Word_GetText");
        names.Should().Contain("ConvertToPdf");
        names.Should().Contain("PowerPoint_GetText");
        names.Should().Contain("PDF_MergePdfs");
        names.Count.Should().BeGreaterThan(25, "clerk document tool registry should cover PDF/Word/Excel/PPT/OfficeToPdf");
    }

    private sealed class InMemoryFileStorage : Shared.Interfaces.IFileStorageService
    {
        public Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default) =>
            Task.FromResult(fileName);

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream());

        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public string GetFullPath(string storagePath) => storagePath;
    }
}

public class SyncfusionDocumentAgentOrchestratorTests
{
    [Fact]
    public void ParseOrchestrationResult_ParsesJsonPayload()
    {
        var json = """{"extractedText":"Annual budget filing","tablesExtractedCount":2}""";
        var result = SyncfusionDocumentAgentOrchestrator.ParseOrchestrationResult(json, "budget.pdf");

        result.Should().NotBeNull();
        result!.ExtractedText.Should().Be("Annual budget filing");
        result.TablesExtractedCount.Should().Be(2);
        result.UsedSyncfusionTools.Should().BeTrue();
    }

    [Fact]
    public void ParseOrchestrationResult_FallsBackToPlainText()
    {
        var result = SyncfusionDocumentAgentOrchestrator.ParseOrchestrationResult(
            "Plain extraction body from model",
            "memo.docx");

        result!.ExtractedText.Should().Contain("Plain extraction");
    }

    [Fact]
    public async Task TryExtractAsync_ReturnsNullWhenOrchestrationDisabled()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["USE_SYNCFUSION_AGENT_TOOLS"] = "true",
                ["USE_SYNCFUSION_AGENT_ORCHESTRATION"] = "false"
            })
            .Build();

        var ollama = new Mock<IOllamaChatClientFactory>();
        var storage = new NasSyncfusionDocumentStorage(new InMemoryFileStorage());
        var registry = new SyncfusionDocumentAgentToolRegistry(storage);
        var sut = new SyncfusionDocumentAgentOrchestrator(
            ollama.Object, registry, config, NullLogger<SyncfusionDocumentAgentOrchestrator>.Instance);

        var result = await sut.TryExtractAsync("scan.pdf", "scan.pdf");
        result.Should().BeNull();
        ollama.Verify(f => f.CreateChatClient(), Times.Never);
    }

    [Fact]
    public async Task TryExtractAsync_UsesOllamaWhenEnabled()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["USE_SYNCFUSION_AGENT_TOOLS"] = "true",
                ["USE_SYNCFUSION_AGENT_ORCHESTRATION"] = "true"
            })
            .Build();

        var ollama = new Mock<IOllamaChatClientFactory>();
        ollama.Setup(f => f.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
        ollama.Setup(f => f.CreateChatClient()).Returns(new StubChatClient(
            """{"extractedText":"Ollama extracted deadline","tablesExtractedCount":1}"""));

        var storage = new NasSyncfusionDocumentStorage(new InMemoryFileStorage());
        var registry = new SyncfusionDocumentAgentToolRegistry(storage);
        var sut = new SyncfusionDocumentAgentOrchestrator(
            ollama.Object, registry, config, NullLogger<SyncfusionDocumentAgentOrchestrator>.Instance);

        var result = await sut.TryExtractAsync("agent-scans/sf-work/scan.pdf", "scan.pdf");

        result.Should().NotBeNull();
        result!.ExtractedText.Should().Contain("Ollama extracted");
        ollama.Verify(f => f.CreateChatClient(), Times.Once);
    }

    private sealed class InMemoryFileStorage : Shared.Interfaces.IFileStorageService
    {
        public Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default) =>
            Task.FromResult(fileName);

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream());

        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public string GetFullPath(string storagePath) => storagePath;
    }
}
