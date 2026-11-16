using FluentAssertions;
using TIKR.Web.Helpers;

namespace TIKR.Web.Tests.Helpers;

public class AssistantTurnStopTests
{
    [Fact]
    public void IsUserStop_IsTrueOnlyWhenTheClerkTokenIsCancelled()
    {
        using var user = new CancellationTokenSource();
        AssistantTurnStop.IsUserStop(user.Token).Should().BeFalse();
        user.Cancel();
        AssistantTurnStop.IsUserStop(user.Token).Should().BeTrue();
        AssistantTurnStop.IsUserStop(CancellationToken.None).Should().BeFalse();
    }
}
