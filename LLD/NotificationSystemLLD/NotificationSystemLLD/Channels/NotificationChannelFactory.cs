using NotificationSystemLLD.Domain.Enums;
using NotificationSystemLLD.Interfaces;

namespace NotificationSystemLLD.Channels;

// Factory Pattern: centralizes channel creation.
// To add WhatsApp: create WhatsAppChannel : INotificationChannel, register here.
public class NotificationChannelFactory
{
    private readonly Dictionary<Channel, INotificationChannel> _channels;

    public NotificationChannelFactory(IEnumerable<INotificationChannel> channels) =>
        _channels = channels.ToDictionary(c => c.Channel);

    public INotificationChannel Get(Channel channel) =>
        _channels.TryGetValue(channel, out var ch)
            ? ch
            : throw new NotSupportedException($"Channel '{channel}' is not registered.");
}
