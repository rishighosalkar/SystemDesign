using NotificationSystemLLD.Channels.Vendors;
using NotificationSystemLLD.Domain.Enums;
using NotificationSystemLLD.Domain.Models;
using NotificationSystemLLD.Interfaces;

namespace NotificationSystemLLD.Channels;

public class InAppChannel : INotificationChannel
{
    private readonly InAppVendor _vendor;

    public Channel Channel => Channel.InApp;

    public InAppChannel(InAppVendor vendor) => _vendor = vendor;

    public async Task<bool> SendAsync(Notification notification, User user) =>
        await _vendor.StoreAsync(user.Id, notification.RenderedBody);
}
