using FluentAssertions;
using TIKR.Infrastructure.Services;

namespace TIKR.Infrastructure.Tests.Services;

public class InteractiveAiGateTests
{
    [Fact]
    public async Task WaitWhileChat_ReturnsAfterRelease()
    {
        var gate = new InteractiveAiGate();
        gate.Hold(TimeSpan.FromSeconds(30));
        gate.ChatInFlight.Should().BeTrue();

        var waiting = gate.WaitWhileChatAsync(CancellationToken.None);
        gate.Release();

        await waiting.WaitAsync(TimeSpan.FromSeconds(2));
        gate.ChatInFlight.Should().BeFalse();
    }

    [Fact]
    public async Task RunYielding_RetriesAfterAChatHoldCancelsTheFirstAttempt()
    {
        var gate = new InteractiveAiGate();
        var calls = 0;
        _ = Task.Run(async () =>
        {
            while (calls == 0)
                await Task.Delay(20);
            await Task.Delay(500);
            gate.Release();
        });

        var result = await gate.RunYieldingToChatAsync(async token =>
        {
            calls++;
            if (calls == 1)
            {
                gate.Hold(TimeSpan.FromSeconds(30));
                await Task.Delay(Timeout.Infinite, token);
            }

            return "ok";
        }, CancellationToken.None);

        result.Should().Be("ok");
        calls.Should().Be(2);
    }
}
