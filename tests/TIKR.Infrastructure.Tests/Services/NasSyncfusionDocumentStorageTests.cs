using FluentAssertions;
using TIKR.Infrastructure.Services;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Tests.Services;

public class NasSyncfusionDocumentStorageTests
{
    [Fact]
    public void WriteThenRead_RoundTripsBytes()
    {
        var fileStorage = new InMemoryFileStorage();
        var sut = new NasSyncfusionDocumentStorage(fileStorage);
        var payload = "syncfusion work file"u8.ToArray();

        sut.Write("scan.pdf", new MemoryStream(payload));
        using var read = sut.Read("scan.pdf");
        using var ms = new MemoryStream();
        read.CopyTo(ms);

        ms.ToArray().Should().Equal(payload);
        fileStorage.SavedFileNames.Should().ContainSingle().Which.Should().Be("agent-scans/sf-work/scan.pdf");
    }

    [Fact]
    public void Exists_ReturnsTrueAfterWrite()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "tikr-sf-storage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var fileStorage = new TempFileStorage(tempRoot);
            var sut = new NasSyncfusionDocumentStorage(fileStorage);

            sut.Exists("present.pdf").Should().BeFalse();
            sut.Write("present.pdf", new MemoryStream("x"u8.ToArray()));
            sut.Exists("present.pdf").Should().BeTrue();
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private sealed class InMemoryFileStorage : IFileStorageService
    {
        private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

        public List<string> SavedFileNames { get; } = [];

        public Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
        {
            SavedFileNames.Add(fileName);
            using var ms = new MemoryStream();
            content.CopyTo(ms);
            _files[fileName] = ms.ToArray();
            return Task.FromResult(fileName);
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

    private sealed class TempFileStorage(string root) : IFileStorageService
    {
        public List<string> SavedFileNames { get; } = [];

        public async Task<string> SaveAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
        {
            SavedFileNames.Add(fileName);
            var full = GetFullPath(fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            await using var fs = File.Create(full);
            await content.CopyToAsync(fs, cancellationToken);
            return fileName;
        }

        public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default) =>
            Task.FromResult<Stream>(File.OpenRead(GetFullPath(storagePath)));

        public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
        {
            File.Delete(GetFullPath(storagePath));
            return Task.CompletedTask;
        }

        public string GetFullPath(string storagePath) =>
            Path.Combine(root, storagePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
