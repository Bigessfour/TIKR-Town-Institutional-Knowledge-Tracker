using FluentAssertions;
using TIKR.Infrastructure.Services;
using TIKR.Shared.TestFixtures;

namespace TIKR.Infrastructure.Tests.Services;

[Trait("Category", TestCategories.FullyTested)]
public class FolderClassifierTests
{
    private static string FixturePath =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "folder-classifier.json");

    [Fact]
    public void Load_MissingPath_ReturnsNoPrediction()
    {
        var classifier = FolderClassifier.Load(null);
        classifier.Classify("budget.pdf", "annual budget", null).Should().BeNull();
    }

    [Fact]
    public void Classify_ElectionManual_IsGeneralNotPersonnel()
    {
        var classifier = FolderClassifier.Load(FixturePath);
        var prediction = classifier.Classify(
            "2025 Special District Election Manual.pdf",
            "This manual explains the special district election, mail ballot procedures, and the canvass board. It is not a personnel file.",
            "ELECTIONS/2025 Special District Election Manual.pdf");

        prediction.Should().NotBeNull();
        prediction!.Value.Folder.Should().Be(DocumentTagHeuristics.General);
        prediction.Value.Folder.Should().NotBe(DocumentTagHeuristics.PersonnelHr);
        prediction.Value.IsConfident.Should().BeTrue();
    }

    [Fact]
    public void Classify_Resume_IsPersonnel()
    {
        var classifier = FolderClassifier.Load(FixturePath);
        var prediction = classifier.Classify(
            "Stephen_Resume.pdf",
            "Curriculum vitae. Experience: deputy clerk. Employment history and payroll.",
            "PERSONNEL/Stephen_Resume.pdf");

        prediction.Should().NotBeNull();
        prediction!.Value.Folder.Should().Be(DocumentTagHeuristics.PersonnelHr);
        prediction.Value.IsConfident.Should().BeTrue();
    }

    [Fact]
    public void Classify_VagueNote_IsNotConfident()
    {
        var classifier = FolderClassifier.Load(FixturePath);
        var prediction = classifier.Classify(
            "hello.pdf",
            "hello world miscellaneous note about the weather",
            "NOTES/hello.pdf");

        prediction.Should().NotBeNull();
        prediction!.Value.IsConfident.Should().BeFalse();
    }
}
