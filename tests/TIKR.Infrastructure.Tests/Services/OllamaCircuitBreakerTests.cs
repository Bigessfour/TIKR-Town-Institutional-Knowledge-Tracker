using FluentAssertions;
using Microsoft.Extensions.Configuration;
using TIKR.Infrastructure.Services;

namespace TIKR.Infrastructure.Tests.Services;

public class OllamaCircuitBreakerTests
{
    private static OllamaCircuitBreaker Create(int failures = 3, int coolSeconds = 60) =>
        new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["TIKR_OLLAMA_CIRCUIT_FAILURES"] = failures.ToString(),
            ["TIKR_OLLAMA_CIRCUIT_COOL_SECONDS"] = coolSeconds.ToString()
        }).Build());

    [Fact]
    public void OpensAfterConfiguredFailures_AndClosesOnSuccess()
    {
        var breaker = Create(failures: 3, coolSeconds: 30);

        breaker.IsOpen.Should().BeFalse();
        breaker.NoteFailure("a");
        breaker.NoteFailure("b");
        breaker.IsOpen.Should().BeFalse();
        breaker.NoteFailure("c");
        breaker.IsOpen.Should().BeTrue();
        breaker.StatusHint.Should().Contain("paused");

        breaker.NoteSuccess();
        breaker.IsOpen.Should().BeFalse();
        breaker.StatusHint.Should().BeNull();
    }

    [Fact]
    public void NoteTimeout_SetsLastCallTimedOutHint()
    {
        var breaker = Create(failures: 10);
        breaker.NoteTimeout("embedding timeout");
        breaker.LastCallTimedOut.Should().BeTrue();
        breaker.StatusHint.Should().Contain("timed out");
    }
}
