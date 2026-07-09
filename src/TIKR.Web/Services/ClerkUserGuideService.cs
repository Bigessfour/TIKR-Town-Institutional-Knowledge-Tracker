namespace TIKR.Web.Services;

public sealed class ClerkUserGuideService(IWebHostEnvironment env)
{
    private IReadOnlyList<ClerkGuideSection>? _sections;
    private string? _rawMarkdown;

    public event Action? OpenRequested;

    public void Open() => OpenRequested?.Invoke();

    public async Task<string> GetMarkdownAsync()
    {
        if (_rawMarkdown is not null)
            return _rawMarkdown;

        var path = Path.Combine(env.WebRootPath, "help", "clerk-user-guide.md");
        _rawMarkdown = File.Exists(path)
            ? await File.ReadAllTextAsync(path)
            : "# Guide\n\nHelp file not found.";
        return _rawMarkdown;
    }

    public async Task<IReadOnlyList<ClerkGuideSection>> GetSectionsAsync()
    {
        if (_sections is not null)
            return _sections;

        var md = await GetMarkdownAsync();
        _sections = ClerkGuideContent.ParseSections(md);
        return _sections;
    }
}
