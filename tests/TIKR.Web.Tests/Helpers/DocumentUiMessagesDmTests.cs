using FluentAssertions;
using TIKR.Web.Helpers;

namespace TIKR.Web.Tests.Helpers;

public class DocumentUiMessagesDmTests
{
    [Theory]
    [InlineData("a.pdf", "e-export-pdf")]
    [InlineData("memo.docx", "e-file-document")]
    [InlineData("rates.xlsx", "e-table")]
    [InlineData("scan.png", "e-image")]
    [InlineData("note.txt", "e-description")]
    [InlineData("unknown.bin", "e-file")]
    public void FileTypeIconCss_MapsCommonExtensions(string name, string expectedFragment)
    {
        DocumentUiMessages.FileTypeIconCss(name).Should().Contain(expectedFragment);
    }

    [Fact]
    public void SoftDeleteAndRestoreMessages_AreClerkFriendly()
    {
        DocumentUiMessages.SoftDeleted("agenda.pdf").Should().Contain("Recycle bin");
        DocumentUiMessages.Restored("agenda.pdf").Should().Contain("Restored");
        DocumentUiMessages.Purged("agenda.pdf").Should().Contain("Permanently");
        DocumentUiMessages.VersionRestored(2).Should().Contain("version 2");
    }
}
