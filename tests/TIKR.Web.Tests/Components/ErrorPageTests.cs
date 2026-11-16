using Bunit;
using FluentAssertions;
using TIKR.Web.Components.Pages;

namespace TIKR.Web.Tests.Components;

public class ErrorPageTests : TestContext
{
    [Fact]
    public void Error_ShowsAClerkSentence()
    {
        var cut = RenderComponent<Error>();

        cut.Markup.Should().Contain("Something went wrong.");
        cut.Markup.Should().Contain("Go back and try again.");
        cut.Markup.Should().NotContain("Request ID");
        cut.Markup.Should().NotContain("Development Mode");
    }
}
