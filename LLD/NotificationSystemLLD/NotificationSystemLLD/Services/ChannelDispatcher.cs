using NotificationSystemLLD.Channels;
using NotificationSystemLLD.Domain.Enums;
using NotificationSystemLLD.Interfaces;

namespace NotificationSystemLLD.Services;

// Pulls notifications from the queue and dispatches them to the correct channel.
// Applies exponential backoff retry on failure.
// In production this runs as a background worker / consumer.
public class ChannelDispatcher
{
    private readonly IMessageQueue _queue;
    private readonly IUserRepository _userRepo;
    private readonly NotificationChannelFactory _channelFactory;
    private readonly IRetryPolicy _retryPolicy;
    private readonly StatusTracker _statusTracker;

    public ChannelDispatcher(
        IMessageQueue queue,
        IUserRepository userRepo,
        NotificationChannelFactory channelFactory,
        IRetryPolicy retryPolicy,
        StatusTracker statusTracker)
    {
        _queue          = queue;
        _userRepo       = userRepo;
        _channelFactory = channelFactory;
        _retryPolicy    = retryPolicy;
        _statusTracker  = statusTracker;
    }

    public async Task ProcessQueueAsync()
    {
        while (!_queue.IsEmpty)
        {
            var notification = _queue.Dequeue();
            if (notification is null) continue;

            var user = _userRepo.GetById(notification.UserId);
            if (user is null) continue;

            var channel = _channelFactory.Get(notification.Channel);
            var success = false;

            // Retry loop with exponential backoff
            while (!success && _retryPolicy.ShouldRetry(notification.RetryCount))
            {
                if (notification.RetryCount > 0)
                {
                    var delay = _retryPolicy.GetDelay(notification.RetryCount);
                    Console.WriteLine($"[Dispatcher] Retry {notification.RetryCount} — waiting {delay.TotalSeconds}s");
                    await Task.Delay(delay);
                }

                success = await channel.SendAsync(notification, user);

                if (!success) notification.RetryCount++;
            }

            var finalStatus = success ? NotificationStatus.Sent : NotificationStatus.Failed;
            _statusTracker.Transition(notification.NotificationId, finalStatus);
        }
    }
}
