using FluentAssertions;
using TIKR.Shared.DTOs;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Services;

public class DashboardLayoutServiceTests
{
    [Fact]
    public void StorageKey_DropsSavedV1Layout()
    {
        DashboardLayoutService.StorageKey.Should().Be("tikr-dashboard-layout-v2");
    }

    [Fact]
    public void DefaultPanels_HasExpectedIds()
    {
        var ids = DashboardLayoutService.DefaultPanels.Select(p => p.PanelId).ToList();
        ids.Should().Contain("due-out-grid");
        ids.Should().Contain("urgency-strip");
        ids.Should().Contain("quick-actions");
    }

    [Fact]
    public void DefaultPanels_DoNotOccupyTheSameCell()
    {
        var cells = new HashSet<(int Column, int Row)>();
        foreach (var panel in DashboardLayoutService.DefaultPanels)
        {
            for (var row = panel.Row; row < panel.Row + panel.SizeY; row++)
            {
                for (var column = panel.Column; column < panel.Column + panel.SizeX; column++)
                {
                    cells.Add((column, row))
                        .Should()
                        .BeTrue($"{panel.PanelId} overlaps column {column}, row {row}");
                }
            }
        }
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
