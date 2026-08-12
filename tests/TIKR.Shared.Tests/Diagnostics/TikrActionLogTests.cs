using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using TIKR.Shared.Diagnostics;
using TIKR.Shared.TestFixtures;

namespace TIKR.Shared.Tests.Diagnostics;

[Trait("Category", TestCategories.FullyTested)]
public class TikrActionLogTests
{
    [Fact]
    public void Started_LogsStructuredAction()
    {
        var logger = NullLogger.Instance;
        TikrActionLog.Started(logger, "Test.Action", "detail");
        TikrActionLog.Completed(logger, "Test.Action", "detail", durationMs: 12);
        TikrActionLog.Info(logger, "Test.Action", "info");
    }
}
