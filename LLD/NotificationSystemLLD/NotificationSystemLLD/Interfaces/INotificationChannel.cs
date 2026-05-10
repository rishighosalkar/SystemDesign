using NotificationSystemLLD.Domain.Enums;
using NotificationSystemLLD.Domain.Models;

namespace NotificationSystemLLD.Interfaces;

// Strategy Pattern: every channel implements this contract.
// The dispatcher talks only to this interface — never to concrete channels.
public interface INotificationChannel
{
    Channel Channel { get; }
    Task<bool> SendAsync(Notification notification, User user);
}
