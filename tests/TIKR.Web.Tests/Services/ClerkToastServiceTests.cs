using FluentAssertions;
using TIKR.Web.Services;

namespace TIKR.Web.Tests.Services;

public class ClerkToastServiceTests
{
    [Fact]
    public void Show_RaisesOnShowEvent()
    {
        var service = new ClerkToastService();
        ClerkToastMessage? captured = null;
        service.OnShow += message => captured = message;

        service.Show("Deleted item", () => Task.CompletedTask);

        captured.Should().NotBeNull();
        captured!.Message.Should().Be("Deleted item");
        captured.UndoAsync.Should().NotBeNull();
    }
}
