namespace TIKR.Infrastructure.Services;

/// <summary>
/// Process-wide single-flight lock for embedding reindex (auto-recovery + manual).
/// Registered as a singleton so hosted recovery and API handlers share one gate.
/// </summary>
public sealed class EmbeddingReindexGate
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public bool IsBusy => _gate.CurrentCount == 0;

    public Task<bool> TryEnterAsync(CancellationToken cancellationToken = default) =>
        _gate.WaitAsync(TimeSpan.Zero, cancellationToken);

    public void Exit() => _gate.Release();
}
