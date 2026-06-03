using NotificationSystemLLD.Domain.Enums;
using NotificationSystemLLD.Domain.Models;
using NotificationSystemLLD.Interfaces;

namespace NotificationSystemLLD.Infrastructure.Repositories;

public class InMemoryNotificationRepository : INotificationRepository
{
    private readonly Dictionary<int, Notification> _store = [];
    private int _idCounter = 1;

    public void Save(Notification notification)
    {
        if (notification.NotificationId == 0)
            notification.NotificationId = _idCounter++;
        _store[notification.NotificationId] = notification;
    }

    public Notification? GetById(int notificationId) =>
        _store.TryGetValue(notificationId, out var n) ? n : null;

    public void UpdateStatus(int notificationId, NotificationStatus status)
    {
        if (_store.TryGetValue(notificationId, out var n))
            n.Status = status;
    }
}
