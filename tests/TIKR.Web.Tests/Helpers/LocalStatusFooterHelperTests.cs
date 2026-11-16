using FluentAssertions;
using TIKR.Shared.DTOs;
using TIKR.Web.Helpers;

namespace TIKR.Web.Tests.Helpers;

public class LocalStatusFooterHelperTests
{
    private static readonly DateTime Now = new(2026, 6, 28, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void BuildFooterMessage_IncludesTownStorageAndLastSaved()
    {
        var status = new LocalStorageStatusDto("Wiley", "Synology NAS", Now.AddMinutes(-12), true);
        LocalStatusFooterHelper.BuildFooterMessage(status, Now)
            .Should().Be("Town files stay on town storage • Wiley • Last saved 12 min ago");
    }

    [Theory]
    [InlineData(0, "Last saved just now")]
    [InlineData(45, "Last saved 45 min ago")]
    [InlineData(120, "Last saved 2 hr ago")]
    public void FormatLastSaved_UsesFriendlyAge(int minutesAgo, string expected)
    {
        LocalStatusFooterHelper.FormatLastSaved(Now.AddMinutes(-minutesAgo), Now)
            .Should().Be(expected);
    }

    [Fact]
    public void BuildConnectionHint_OfflineShowsNasMessage()
    {
        LocalStatusFooterHelper.BuildConnectionHint(apiOffline: true, helperReady: false)
            .Should().Be("The town server is offline. Files stay on town storage.");
    }

    [Fact]
    public void BuildConnectionHint_OnlineWithOllama()
    {
        LocalStatusFooterHelper.BuildConnectionHint(apiOffline: false, helperReady: true)
            .Should().Be("Town files stay on town storage. The helper is ready.");
    }
}
