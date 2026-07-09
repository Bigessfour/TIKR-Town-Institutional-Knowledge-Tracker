using TIKR.Web.Services;

namespace TIKR.Web.Tests.Services;

public sealed class ClerkGuideContentTests
{
    [Fact]
    public void ParseSections_splits_on_h2_headings()
    {
        const string md = """
            # Title
            ## Dashboard
            Dash body.
            ## Documents
            Doc body.
            """;

        var sections = ClerkGuideContent.ParseSections(md);
        Assert.Equal(2, sections.Count);
        Assert.Equal("dashboard", sections[0].Id);
        Assert.Equal("Dashboard", sections[0].Title);
        Assert.Contains("Dash body", sections[0].BodyMarkdown);
    }

    [Fact]
    public void Filter_matches_title_or_body()
    {
        var sections = ClerkGuideContent.ParseSections("""
            ## AI Assistant
            Local Ollama chat.
            ## Calendar
            Read-only schedule.
            """);

        var filtered = ClerkGuideContent.Filter(sections, "ollama");
        Assert.Single(filtered);
        Assert.Equal("AI Assistant", filtered[0].Title);
    }
}
