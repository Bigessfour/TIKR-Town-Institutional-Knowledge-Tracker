namespace TIKR.Infrastructure.Services;

/// <summary>
/// Lets a clerk question go ahead of bulk embedding. Ollama on the NAS runs one
/// request at a time, so a reindex that is already in flight is cancelled and
/// resumed after the question.
/// </summary>
public sealed class InteractiveAiGate
{
    private long _busyUntilTicks;

    public void Hold(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            Release();
            return;
        }

        var until = DateTime.UtcNow.Add(duration).Ticks;
        while (true)
        {
            var current = Interlocked.Read(ref _busyUntilTicks);
            if (current >= until)
                return;
            if (Interlocked.CompareExchange(ref _busyUntilTicks, until, current) == current)
                return;
        }
    }

    public void Release() => Interlocked.Exchange(ref _busyUntilTicks, 0);

    public bool ChatInFlight =>
        new DateTime(Interlocked.Read(ref _busyUntilTicks), DateTimeKind.Utc) > DateTime.UtcNow;

    public async Task WaitWhileChatAsync(CancellationToken cancellationToken)
    {
        while (ChatInFlight)
            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
    }

    public async Task<T> RunYieldingToChatAsync<T>(
        Func<CancellationToken, Task<T>> work,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            await WaitWhileChatAsync(cancellationToken);
            using var attempt = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            using var stop = new CancellationTokenSource();
            var poll = Task.Run(async () =>
            {
                try
                {
                    while (!stop.IsCancellationRequested)
                    {
                        if (ChatInFlight)
                        {
                            await attempt.CancelAsync();
                            return;
                        }

                        await Task.Delay(200, stop.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    /* poll stopped */
                }
            }, CancellationToken.None);

            try
            {
                return await work(attempt.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && ChatInFlight)
            {
                /* question started; try this document again after it finishes */
            }
            finally
            {
                await stop.CancelAsync();
                try
                {
                    await poll;
                }
                catch (OperationCanceledException)
                {
                    /* poll stopped */
                }
            }
        }
    }
}
