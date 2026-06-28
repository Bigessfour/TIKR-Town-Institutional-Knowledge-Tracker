using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TIKR.Infrastructure.Services;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Tests.Services;

public class NasAgentDocumentStorageTests
{
    [Fact]
    public async Task SaveAgentScanAsync_UsesAgentScansPrefix()
    {
        var fileStorage = new InMemoryFileStorage();
        var config = new ConfigurationBuilder().Build();
        var sut = new NasAgentDocumentStorage(fileStorage, config);
        await using var content = new MemoryStream("plain text"u8.ToArray());

        var path = await sut.SaveAgentScanAsync(content, "report.txt");

        path.Should().StartWith("agent-scans/");
        path.Should().EndWith("report.txt");
        fileStorage.SavedFileNames.Should().ContainSingle().Which.Should().Be("agent-scans/report.txt");
    }

    [Fact]
    public async Task SaveAgentScanAsync_EncryptsWhenKeyConfigured()
    {
        var fileStorage = new InMemoryFileStorage();
        var key = Convert.ToBase64String(new byte[AgentStorageCrypto.KeySize]);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["TIKR_AGENT_STORAGE_KEY"] = key })
            .Build();
        var sut = new NasAgentDocumentStorage(fileStorage, config);
        await using var content = new MemoryStream("secret clerk note"u8.ToArray());

        var path = await sut.SaveAgentScanAsync(content, "note.txt");

        path.Should().EndWith(".agentenc");
        fileStorage.LastSavedBytes.Should().NotBeNull();
        fileStorage.LastSavedBytes!.Should().NotEqual("secret clerk note"u8.ToArray());
    }

    private sealed class InMemoryFileStorage : IFileStorageService
    {
        public List<string> SavedFileNames { get; } = [];
        public byte[]? LastSavedBytes { get; private set; }

        public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
        {
            SavedFileNames.Add(fileName);
            await using var ms = new MemoryStream();
            await content.CopyToAsync(ms, cancellationToken);
            LastSavedBytes = ms.ToArray();
            return fileName;
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(new MemoryStream(LastSavedBytes ?? []));

        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public string GetFullPath(string storagePath) => storagePath;
    }
}
