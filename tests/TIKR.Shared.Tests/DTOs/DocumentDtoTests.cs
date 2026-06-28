using FluentAssertions;
using TIKR.Shared.DTOs;

namespace TIKR.Shared.Tests.DTOs;

public class DocumentDtoTests
{
    [Fact]
    public void Constructor_PreservesAllFields()
    {
        var id = Guid.NewGuid();
        var uploaded = DateTime.UtcNow;
        var dto = new DocumentDto(id, "minutes.pdf", "application/pdf", 4096, "[\"gov\"]", "Council", uploaded);

        dto.Id.Should().Be(id);
        dto.FileName.Should().Be("minutes.pdf");
        dto.ContentType.Should().Be("application/pdf");
        dto.FileSizeBytes.Should().Be(4096);
        dto.AiTags.Should().Be("[\"gov\"]");
        dto.SuggestedFolder.Should().Be("Council");
        dto.UploadedAt.Should().Be(uploaded);
    }
}
