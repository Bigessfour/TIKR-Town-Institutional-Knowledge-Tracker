using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TIKR.Infrastructure.Services;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Tests.Services;

public class SyncfusionDocumentAgentExtractionBackendTests
{
    [Fact]
    public async Task ExtractAsync_DelegatesToExtractor()
    {
        var storage = new NasSyncfusionDocumentStorage(new InMemoryFileStorage());
        var config = new ConfigurationBuilder().Build();
        var ollama = Mock.Of<IOllamaChatClientFactory>();
        var registry = new SyncfusionDocumentAgentToolRegistry(storage);
        var searchTools = CreateSearchTools();
        var orchestrator = new SyncfusionDocumentAgentOrchestrator(
            ollama, registry, searchTools, config, NullLogger<SyncfusionDocumentAgentOrchestrator>.Instance);
        var ocr = new DisabledOcr();
        var extractor = new SyncfusionDocumentAgentExtractor(storage, orchestrator, ocr);
        var sut = new SyncfusionDocumentAgentExtractionBackend(extractor);
        await using var content = new MemoryStream("delegated text"u8.ToArray());

        var result = await sut.ExtractAsync(content, "note.txt");

        result.ExtractedText.Should().Contain("delegated text");
    }

    private static TownDocumentSearchToolRegistry CreateSearchTools()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Mock.Of<IHybridAiService>());
        var provider = services.BuildServiceProvider();
        return new TownDocumentSearchToolRegistry(provider.GetRequiredService<IServiceScopeFactory>());
    }

    private sealed class DisabledOcr : IDocumentOcrService
    {
        public bool IsEnabled => false;
        public DocumentOcrResult EnrichPdf(Stream pdfContent, string? existingText, int pageCountHint = 1) =>
            new(existingText ?? string.Empty, false);
        public DocumentOcrResult EnrichWord(Stream wordContent, string fileName, string? existingText) =>
            new(existingText ?? string.Empty, false);
    }

    private sealed class InMemoryFileStorage : Shared.Interfaces.IFileStorageService
    {
        private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

        public Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
        {
            using var ms = new MemoryStream();
            content.CopyTo(ms);
            _files[fileName] = ms.ToArray();
            return Task.FromResult(fileName);
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream(_files[storagePath]));

        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public string GetFullPath(string storagePath) => storagePath;
    }
}
