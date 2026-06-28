using FluentAssertions;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Services;

public class LocalConnectionStateServiceTests
{
    [Fact]
    public void SetApiOffline_RaisesChangedOncePerTransition()
    {
        var service = new LocalConnectionStateService();
        var changes = 0;
        service.Changed += () => changes++;

        service.SetApiOffline(true);
        service.SetApiOffline(true);
        service.SetApiOffline(false);

        changes.Should().Be(2);
        service.ApiOffline.Should().BeFalse();
    }
}
