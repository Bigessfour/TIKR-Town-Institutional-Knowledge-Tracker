using FluentAssertions;
using Microsoft.Extensions.Configuration;
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
        var orchestrator = new SyncfusionDocumentAgentOrchestrator(
            ollama, registry, config, NullLogger<SyncfusionDocumentAgentOrchestrator>.Instance);
        var extractor = new SyncfusionDocumentAgentExtractor(storage, orchestrator);
        var sut = new SyncfusionDocumentAgentExtractionBackend(extractor);
        await using var content = new MemoryStream("delegated text"u8.ToArray());

        var result = await sut.ExtractAsync(content, "note.txt");

        result.ExtractedText.Should().Contain("delegated text");
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
