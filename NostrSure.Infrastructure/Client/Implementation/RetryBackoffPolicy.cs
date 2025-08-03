using NostrSure.Infrastructure.Client.Abstractions;

namespace NostrSure.Infrastructure.Client.Implementation;

/// <summary>
/// Retry policy with exponential backoff
/// </summary>
public class RetryBackoffPolicy : IHealthPolicy
{
    private readonly TimeSpan _baseDelay;
    private readonly TimeSpan _maxDelay;
    private readonly int _maxRetries;
    private readonly Random _jitterRandom;

    public RetryBackoffPolicy(
        TimeSpan? baseDelay = null,
        TimeSpan? maxDelay = null,
        int maxRetries = 5)
    {
        _baseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
        _maxDelay = maxDelay ?? TimeSpan.FromMinutes(1);
        _maxRetries = maxRetries;
        _jitterRandom = new Random();
    }

    public async Task DelayAsync(int attempt, CancellationToken cancellationToken = default)
    {
        if (attempt <= 0) return;

        var delay = GetDelay(attempt);
        await Task.Delay(delay, cancellationToken);
    }

    public bool ShouldRetry(int attempt)
    {
        return attempt <= _maxRetries;
    }

    public TimeSpan GetDelay(int attempt)
    {
        if (attempt <= 0) return TimeSpan.Zero;

        // Exponential backoff: baseDelay * 2^(attempt-1)
        var exponentialDelay = TimeSpan.FromTicks(
            _baseDelay.Ticks * (1L << Math.Min(attempt - 1, 10))); // Cap at 2^10 to prevent overflow

        // Cap at max delay
        var cappedDelay = exponentialDelay > _maxDelay ? _maxDelay : exponentialDelay;

        // Add jitter (±25% of the delay)
        var jitterRange = (int)(cappedDelay.TotalMilliseconds * 0.25);
        var jitter = _jitterRandom.Next(-jitterRange, jitterRange + 1);
        var finalDelay = cappedDelay.Add(TimeSpan.FromMilliseconds(jitter));

        // Ensure non-negative
        return finalDelay > TimeSpan.Zero ? finalDelay : TimeSpan.Zero;
    }
}