using FluentAssertions;
using Microsoft.Extensions.Logging;
using TIKR.Shared.Diagnostics;
using TIKR.Shared.TestFixtures;

namespace TIKR.Shared.Tests.Diagnostics;

[Trait("Category", TestCategories.FullyTested)]
public class TikrActionLogTests
{
    [Fact]
    public void Started_Completed_Failed_EmitExpectedMessages()
    {
        var logger = new CaptureLogger();

        TikrActionLog.Started(logger, "UI.Test.Load");
        TikrActionLog.Completed(logger, "UI.Test.Load", "Count=3", durationMs: 12);
        TikrActionLog.Failed(logger, "UI.Test.Load", "boom", "Detail=x");
        TikrActionLog.Info(logger, "UI.Test.Load", "hello");

        logger.Messages.Should().Contain(m => m.Contains("Action") && m.Contains("started"));
        logger.Messages.Should().Contain(m => m.Contains("completed") && m.Contains("Count=3"));
        logger.Messages.Should().Contain(m => m.Contains("failed") && m.Contains("boom"));
        logger.Messages.Should().Contain(m => m.Contains("info") && m.Contains("hello"));
    }

    private sealed class CaptureLogger : ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }
}
