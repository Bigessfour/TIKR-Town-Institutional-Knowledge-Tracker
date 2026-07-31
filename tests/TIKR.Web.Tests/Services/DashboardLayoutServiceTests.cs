using FluentAssertions;
using TIKR.Shared.DTOs;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Services;

public class DashboardLayoutServiceTests
{
    [Fact]
    public void DefaultPanels_HasExpectedIds()
    {
        var ids = DashboardLayoutService.DefaultPanels.Select(p => p.PanelId).ToList();
        ids.Should().Contain("due-out-grid");
        ids.Should().Contain("urgency-strip");
        ids.Should().Contain("quick-actions");
    }

    [Fact]
    public void SerializeDeserialize_RoundTrips()
    {
        var json = DashboardLayoutService.Serialize(DashboardLayoutService.DefaultPanels);
        var restored = DashboardLayoutService.TryDeserialize(json);
        restored.Should().NotBeNull();
        restored!.Count.Should().Be(DashboardLayoutService.DefaultPanels.Count);
    }

    [Fact]
    public void TryDeserialize_InvalidJson_ReturnsNull()
    {
        DashboardLayoutService.TryDeserialize("{not-json").Should().BeNull();
    }
}
