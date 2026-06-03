using NotificationSystemLLD.Channels.Vendors;
using NotificationSystemLLD.Domain.Enums;
using NotificationSystemLLD.Domain.Models;
using NotificationSystemLLD.Interfaces;

namespace NotificationSystemLLD.Channels;

public class PushChannel : INotificationChannel
{
    private readonly PushVendor _vendor;

    public Channel Channel => Channel.Push;

    public PushChannel(PushVendor vendor) => _vendor = vendor;

    public async Task<bool> SendAsync(Notification notification, User user) =>
        await _vendor.SendAsync(user.DeviceToken, notification.RenderedBody);
}
