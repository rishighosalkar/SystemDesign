using NotificationSystemLLD.Interfaces;

namespace NotificationSystemLLD.Infrastructure.Retry;

// Exponential Backoff: delay = baseDelay * 2^retryCount
// Attempt 1 → 2s, Attempt 2 → 4s, Attempt 3 → 8s, then FAILED.
public class ExponentialBackoffRetryPolicy : IRetryPolicy
{
    private readonly TimeSpan _baseDelay;

    public int MaxRetries { get; }

    public ExponentialBackoffRetryPolicy(int maxRetries = 3, int baseDelaySeconds = 2)
    {
        MaxRetries = maxRetries;
        _baseDelay = TimeSpan.FromSeconds(baseDelaySeconds);
    }

    public bool ShouldRetry(int retryCount) => retryCount < MaxRetries;

    public TimeSpan GetDelay(int retryCount) =>
        _baseDelay * Math.Pow(2, retryCount);
}
