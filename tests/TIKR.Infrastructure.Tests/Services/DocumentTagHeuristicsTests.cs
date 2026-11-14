using FluentAssertions;
using TIKR.Infrastructure.Services;
using TIKR.Shared.TestFixtures;

namespace TIKR.Infrastructure.Tests.Services;

[Trait("Category", TestCategories.FullyTested)]
public class DocumentTagHeuristicsTests
{
    [Theory]
    [InlineData("Stephen_Resume.pdf", null, DocumentTagHeuristics.PersonnelHr)]
    [InlineData("jane-cv.docx", null, DocumentTagHeuristics.PersonnelHr)]
    [InlineData("budget-2026.pdf", null, DocumentTagHeuristics.BudgetFinance)]
    [InlineData("Ordinance_12.pdf", null, DocumentTagHeuristics.Ordinances)]
    [InlineData("council-minutes.pdf", null, DocumentTagHeuristics.Minutes)]
    [InlineData("7-july-agenda.docx", null, DocumentTagHeuristics.Agenda)]
    public void FillGaps_InfersFolderFromFilename(string fileName, string? content, string expectedFolder)
    {
        var (tags, folder) = DocumentTagHeuristics.FillGaps(fileName, content, [], null);
        folder.Should().Be(expectedFolder);
        tags.Should().NotBeEmpty();
    }

    [Fact]
    public void FillGaps_DoesNotOverrideAiFolderOrTags()
    {
        var (tags, folder) = DocumentTagHeuristics.FillGaps(
            "Stephen_Resume.pdf",
            null,
            ["custom"],
            "Correspondence");

        tags.Should().BeEquivalentTo(["custom"]);
        folder.Should().Be("Correspondence");
    }

    [Fact]
    public void FillGaps_UsesContentWhenFilenameGeneric()
    {
        var (tags, folder) = DocumentTagHeuristics.FillGaps(
            "document.pdf",
            "Curriculum Vitae\nExperience: Town Clerk",
            [],
            null);

        folder.Should().Be(DocumentTagHeuristics.PersonnelHr);
        tags.Should().Contain("resume");
    }

    [Theory]
    [InlineData("COUNCIL MEETINGS/2024/agenda.pdf", DocumentTagHeuristics.Agenda)]
    [InlineData("COUNCIL MEETINGS/2024-08-12 minutes.pdf", DocumentTagHeuristics.Minutes)]
    [InlineData("ORDINANCES/water-rate.pdf", DocumentTagHeuristics.Ordinances)]
    [InlineData("MUNICIPAL CODE/title-5.pdf", DocumentTagHeuristics.Ordinances)]
    [InlineData("BUDGET/2026.pdf", DocumentTagHeuristics.BudgetFinance)]
    [InlineData("FINANCE/ledger.pdf", DocumentTagHeuristics.BudgetFinance)]
    [InlineData("MILL LEVY/cert.pdf", DocumentTagHeuristics.BudgetFinance)]
    [InlineData("CONTRACTS/vendor.pdf", DocumentTagHeuristics.Contracts)]
    [InlineData("AGREEMENTS/mou.pdf", DocumentTagHeuristics.Contracts)]
    [InlineData("PERSONNEL/handbook.pdf", DocumentTagHeuristics.PersonnelHr)]
    [InlineData("HR/offer.pdf", DocumentTagHeuristics.PersonnelHr)]
    [InlineData("CORRESPONDENCE/letter.pdf", DocumentTagHeuristics.Correspondence)]
    [InlineData("LETTERS/resident.pdf", DocumentTagHeuristics.Correspondence)]
    [InlineData("FORMS/permit.pdf", DocumentTagHeuristics.Forms)]
    [InlineData("BUDGET COMPARISON/2026.pdf", DocumentTagHeuristics.BudgetFinance)]
    [InlineData("RESOLUTIONS&ORDINANCE/res-12.pdf", DocumentTagHeuristics.Ordinances)]
    [InlineData("EMPLOYEE INFO/handbook.pdf", DocumentTagHeuristics.PersonnelHr)]
    [InlineData("ELECTION INFORMATION/2025 Special District Election Manual.pdf", DocumentTagHeuristics.General)]
    [InlineData("ELECTIONS/calendar.pdf", DocumentTagHeuristics.General)]
    [InlineData("ELECTIONS/notice.pdf", DocumentTagHeuristics.General)]
    [InlineData("2025 ELECTIONS/notice.pdf", DocumentTagHeuristics.General)]
    public void TryMapNasRelativePath_MapsWileyFolders(string relativePath, string expected)
    {
        DocumentTagHeuristics.TryMapNasRelativePath(relativePath).Should().Be(expected);
    }

    [Fact]
    public void TryReadUnmappedTopFolder_ProposesClerkFolderAndSkipsKnownRoots()
    {
        DocumentTagHeuristics.TryReadUnmappedTopFolder("FIRE DEPARTMENT/notes.pdf")
            .Should().Be("FIRE DEPARTMENT");
        DocumentTagHeuristics.TryReadUnmappedTopFolder("COUNCIL MEETINGS/packet.pdf").Should().BeNull();
        DocumentTagHeuristics.TryReadUnmappedTopFolder("agenda.pdf").Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("agenda.pdf")]
    [InlineData("COUNCIL MEETINGS/packet.pdf")]
    [InlineData("COUNCIL MEETINGS/last-minute-notes.pdf")]
    [InlineData("RANDOM FOLDER/file.pdf")]
    [InlineData("SELECTION COMMITTEE/notes.pdf")]
    public void TryMapNasRelativePath_ReturnsNullWhenNotConfident(string? relativePath)
    {
        DocumentTagHeuristics.TryMapNasRelativePath(relativePath).Should().BeNull();
    }

    [Fact]
    public void FillGaps_InfersElectionFromFilenameWithoutOverridingModelFolder()
    {
        var (tags, folder) = DocumentTagHeuristics.FillGaps(
            "2025 Special District Election Manual.pdf",
            null,
            [],
            null);

        folder.Should().Be(DocumentTagHeuristics.General);
        tags.Should().Contain("election");

        var kept = DocumentTagHeuristics.FillGaps(
            "2025 Special District Election Manual.pdf",
            null,
            ["personnel"],
            DocumentTagHeuristics.PersonnelHr);

        kept.Tags.Should().BeEquivalentTo(["personnel"]);
        kept.Folder.Should().Be(DocumentTagHeuristics.PersonnelHr);
    }

    [Fact]
    public void FillGaps_PayrollFileMentioningElectionStaysPersonnel()
    {
        var fromName = DocumentTagHeuristics.FillGaps("election-judge-payroll.pdf", null, [], null);
        fromName.Folder.Should().Be(DocumentTagHeuristics.PersonnelHr);

        var fromBody = DocumentTagHeuristics.FillGaps(
            "scan.pdf",
            "Election judge payroll schedule for the special district.",
            [],
            null);
        fromBody.Folder.Should().Be(DocumentTagHeuristics.PersonnelHr);
        fromBody.Tags.Should().NotContain("election");
    }

    [Fact]
    public void FillGaps_DoesNotTreatCouncilFolderPathAsMinutes()
    {
        // Leaf name only — callers must not pass NAS relative paths into FillGaps.
        var (tags, folder) = DocumentTagHeuristics.FillGaps("packet.pdf", null, [], null);
        folder.Should().BeNull();
        tags.Should().BeEmpty();
    }
}
