using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using TIKR.Shared.DTOs;
using TIKR.Shared.TestFixtures;
using TIKR.Web.Components.Shared.Documents;
using TIKR.Web.Helpers;

namespace TIKR.Web.Tests.Components;

[Trait("Category", TestCategories.FullyTested)]
public class DocumentsInspectorIntakeTests : ClerkTestContext
{
    public DocumentsInspectorIntakeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Inspector_ShowsScanForDeadlinesInMoreActions()
    {
        SetRendererInfo(new RendererInfo("Server", true));
        var doc = new DocumentDto(
            Guid.NewGuid(),
            "notice.pdf",
            "application/pdf",
            2048,
            null,
            "Notices",
            DateTime.UtcNow);

        var cut = RenderComponent<DocumentsInspector>(ps => ps
            .Add(p => p.Document, doc)
            .Add(p => p.PreviewKind, DocumentPreviewHelper.PreviewKind.Pdf)
            .Add(p => p.CanOpen, true)
            .Add(p => p.ShowRecycleBin, false));

        var more = cut.FindAll("button").First(b => b.TextContent.Contains("More actions", StringComparison.OrdinalIgnoreCase));
        more.Click();

        cut.WaitForAssertion(() =>
            cut.Markup.Should().Contain("Scan for deadlines"));
    }
}
