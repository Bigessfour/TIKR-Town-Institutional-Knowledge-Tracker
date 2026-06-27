using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TIKR.Infrastructure.Services;

namespace TIKR.Infrastructure.Tests.Services;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _basePath;
    private readonly LocalFileStorageService _sut;

    public LocalFileStorageServiceTests()
    {
        _basePath = Path.Combine(Path.GetTempPath(), $"tikr-storage-{Guid.NewGuid():N}");
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["FILE_STORAGE_PATH"] = _basePath })
            .Build();
        _sut = new LocalFileStorageService(config);
    }

    [Fact]
    public async Task SaveAsync_WritesFileUnderDatedFolder()
    {
        await using var stream = new MemoryStream("hello"u8.ToArray());
        var path = await _sut.SaveAsync(stream, "report.pdf");

        path.Should().Contain("/");
        path.Should().EndWith("_report.pdf");
        File.Exists(_sut.GetFullPath(path)).Should().BeTrue();
    }

    [Fact]
    public async Task OpenReadAsync_ReturnsSavedContent()
    {
        await using var stream = new MemoryStream("payload"u8.ToArray());
        var path = await _sut.SaveAsync(stream, "doc.txt");

        await using var read = await _sut.OpenReadAsync(path);
        using var reader = new StreamReader(read);
        (await reader.ReadToEndAsync()).Should().Be("payload");
    }

    [Fact]
    public async Task DeleteAsync_RemovesExistingFile()
    {
        await using var stream = new MemoryStream("x"u8.ToArray());
        var path = await _sut.SaveAsync(stream, "temp.txt");
        var fullPath = _sut.GetFullPath(path);

        await _sut.DeleteAsync(path);

        File.Exists(fullPath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_DoesNotThrowWhenMissing()
    {
        var act = async () => await _sut.DeleteAsync("missing/file.txt");
        await act.Should().NotThrowAsync();
    }

    public void Dispose()
    {
        if (Directory.Exists(_basePath))
            Directory.Delete(_basePath, recursive: true);
    }
}
