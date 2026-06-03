using NotificationSystemLLD.Channels.Vendors;
using NotificationSystemLLD.Domain.Enums;
using NotificationSystemLLD.Domain.Models;
using NotificationSystemLLD.Interfaces;

namespace NotificationSystemLLD.Channels;

public class EmailChannel : INotificationChannel
{
    private readonly EmailVendor _vendor;

    public Channel Channel => Channel.Email;

    public EmailChannel(EmailVendor vendor) => _vendor = vendor;

    public async Task<bool> SendAsync(Notification notification, User user)
    {
        // Subject was pre-rendered by TemplateRenderer and stored in payload["_subject"]
        var subject = notification.Payload.TryGetValue("_subject", out var s) ? s : "Notification";
        return await _vendor.SendAsync(user.Email, subject, notification.RenderedBody);
    }
}
