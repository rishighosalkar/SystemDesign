using NotificationSystemLLD.Channels.Vendors;
using NotificationSystemLLD.Domain.Enums;
using NotificationSystemLLD.Domain.Models;
using NotificationSystemLLD.Interfaces;

namespace NotificationSystemLLD.Channels;

public class SmsChannel : INotificationChannel
{
    private readonly SmsVendor _vendor;

    public Channel Channel => Channel.Sms;

    public SmsChannel(SmsVendor vendor) => _vendor = vendor;

    public async Task<bool> SendAsync(Notification notification, User user) =>
        await _vendor.SendAsync(user.PhoneNumber, notification.RenderedBody);
}
