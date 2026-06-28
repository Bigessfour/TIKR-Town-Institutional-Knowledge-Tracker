using FluentAssertions;
using TIKR.Web.Helpers;

namespace TIKR.Web.Tests.Helpers;

public class DisplayFormatTests
{
    [Fact]
    public void ParseTags_NullOrWhitespace_ReturnsEmpty()
    {
        DisplayFormat.ParseTags(null).Should().BeEmpty();
        DisplayFormat.ParseTags("   ").Should().BeEmpty();
    }

    [Fact]
    public void ParseTags_JsonArray_Deserializes()
    {
        DisplayFormat.ParseTags("[\"finance\",\"budget\"]")
            .Should().Equal("finance", "budget");
    }

    [Fact]
    public void ParseTags_InvalidJson_FallsBackToCommaSplit()
    {
        DisplayFormat.ParseTags("finance, budget , audit")
            .Should().Equal("finance", "budget", "audit");
    }

    [Theory]
    [InlineData(512, "512 B")]
    [InlineData(2048, "2.0 KB")]
    [InlineData(5 * 1024 * 1024, "5.0 MB")]
    public void FormatBytes_FormatsSizes(long bytes, string expected)
    {
        DisplayFormat.FormatBytes(bytes).Should().Be(expected);
    }

    [Fact]
    public void TruncateForDisplay_ShortText_Unchanged()
    {
        DisplayFormat.TruncateForDisplay("hello").Should().Be("hello");
    }

    [Fact]
    public void TruncateForDisplay_LongText_AddsEllipsis()
    {
        var longText = new string('x', 200);
        DisplayFormat.TruncateForDisplay(longText).Should().HaveLength(180).And.EndWith("...");
    }
}
