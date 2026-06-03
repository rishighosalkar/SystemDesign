namespace NotificationSystemLLD.Interfaces;

// Encapsulates retry decision and delay calculation.
// Exponential backoff: delay = baseDelay * 2^retryCount
public interface IRetryPolicy
{
    int MaxRetries { get; }
    bool ShouldRetry(int retryCount);
    TimeSpan GetDelay(int retryCount);
}
