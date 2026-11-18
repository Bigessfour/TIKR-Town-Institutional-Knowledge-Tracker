using Microsoft.Extensions.Configuration;
using TIKR.Shared.Configuration;

namespace TIKR.Infrastructure.Services;

/// <summary>
/// Process-wide circuit for Ollama chat/embed calls. Fail-soft: when open, callers skip remote work
/// and clerk-facing status explains the pause (local-first: app keeps working without AI).
/// </summary>
public sealed class OllamaCircuitBreaker(IConfiguration configuration)
{
    private readonly object _gate = new();
    private int _consecutiveFailures;
    private DateTime? _openUntilUtc;
    private string? _lastFailureReason;
    private bool _lastCallTimedOut;

    public bool IsOpen
    {
        get
        {
            lock (_gate)
            {
                if (_openUntilUtc is null)
                    return false;
                if (DateTime.UtcNow >= _openUntilUtc.Value)
                    return false; // half-open: allow one trial
                return true;
            }
        }
    }

    public bool LastCallTimedOut
    {
        get
        {
            lock (_gate)
                return _lastCallTimedOut;
        }
    }

    public string? StatusHint
    {
        get
        {
            lock (_gate)
            {
                if (_openUntilUtc is { } until && DateTime.UtcNow < until)
                {
                    var wait = Math.Max(1, (int)Math.Ceiling((until - DateTime.UtcNow).TotalSeconds));
                    return $"Local helper paused after repeated failures — retrying in about {wait}s."
                           + (string.IsNullOrWhiteSpace(_lastFailureReason)
                               ? string.Empty
                               : $" ({_lastFailureReason})");
                }

                if (_lastCallTimedOut)
                    return "Local helper timed out recently — waiting before heavy embedding work.";

                return null;
            }
        }
    }

    public void NoteSuccess()
    {
        lock (_gate)
        {
            _consecutiveFailures = 0;
            _openUntilUtc = null;
            _lastCallTimedOut = false;
            _lastFailureReason = null;
        }
    }

    public void NoteFailure(string? reason = null, bool timedOut = false)
    {
        var threshold = TikrConfiguration.GetOllamaCircuitBreakerFailures(configuration);
        var coolSeconds = TikrConfiguration.GetOllamaCircuitBreakerCoolSeconds(configuration);

        lock (_gate)
        {
            _consecutiveFailures++;
            _lastCallTimedOut = timedOut || _lastCallTimedOut;
            if (!string.IsNullOrWhiteSpace(reason))
                _lastFailureReason = reason.Trim();

            if (_consecutiveFailures >= threshold)
                _openUntilUtc = DateTime.UtcNow.AddSeconds(coolSeconds);
        }
    }

    public void NoteTimeout(string? reason = null) =>
        NoteFailure(reason ?? "timeout", timedOut: true);
}
