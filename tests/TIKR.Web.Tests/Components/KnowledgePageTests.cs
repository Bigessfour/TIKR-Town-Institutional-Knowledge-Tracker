using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using TIKR.Web.Components.Pages;

namespace TIKR.Web.Tests.Components;

public class KnowledgePageTests : TestContext
{
    [Fact]
    public void Knowledge_RedirectsToVault()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        var cut = RenderComponent<Knowledge>();

        nav.Uri.Should().EndWith("/vault");
        cut.Markup.Should().BeEmpty();
    }
}
