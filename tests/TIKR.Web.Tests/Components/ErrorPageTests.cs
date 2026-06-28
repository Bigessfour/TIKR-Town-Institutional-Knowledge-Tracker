using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using TIKR.Web.Components.Pages;

namespace TIKR.Web.Tests.Components;

public class ErrorPageTests : TestContext
{
    [Fact]
    public void Error_ShowsRequestIdFromHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-abc-123";

        var cut = RenderComponent<Error>(p => p.AddCascadingValue(httpContext));

        cut.Markup.Should().Contain("trace-abc-123");
        cut.Markup.Should().Contain("Request ID");
    }
}
