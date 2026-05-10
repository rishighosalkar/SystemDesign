using NotificationSystemLLD.Domain.Enums;
using NotificationSystemLLD.Domain.Models;

namespace NotificationSystemLLD.Interfaces;

public interface INotificationRepository
{
    void Save(Notification notification);
    Notification? GetById(int notificationId);
    void UpdateStatus(int notificationId, NotificationStatus status);
}
