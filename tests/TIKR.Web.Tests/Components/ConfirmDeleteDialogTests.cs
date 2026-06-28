using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using TIKR.Web.Components.Shared;

namespace TIKR.Web.Tests.Components;

public class ConfirmDeleteDialogTests : ClerkTestContext
{
    public ConfirmDeleteDialogTests() => SetRendererInfo(new RendererInfo("Server", true));

    [Fact]
    public void ConfirmDeleteDialog_ShowsItemNameAndAuditNote()
    {
        var cut = RenderComponent<ConfirmDeleteDialog>(parameters => parameters
            .Add(p => p.Visible, true)
            .Add(p => p.ItemName, "Mill Levy Certification"));

        cut.Markup.Should().Contain("Mill Levy Certification");
        cut.Markup.Should().Contain("audit log");
    }
}
