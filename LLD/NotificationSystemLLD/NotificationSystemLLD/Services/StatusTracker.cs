using NotificationSystemLLD.Domain.Enums;
using NotificationSystemLLD.Interfaces;

namespace NotificationSystemLLD.Services;

// State Pattern: enforces valid status transitions.
// QUEUED → SENT → DELIVERED → READ
//                           ↘ FAILED (from QUEUED or SENT)
public class StatusTracker
{
    private static readonly Dictionary<NotificationStatus, HashSet<NotificationStatus>> _validTransitions = new()
    {
        [NotificationStatus.Queued]    = [NotificationStatus.Sent, NotificationStatus.Failed],
        [NotificationStatus.Sent]      = [NotificationStatus.Delivered, NotificationStatus.Failed],
        [NotificationStatus.Delivered] = [NotificationStatus.Read],
        [NotificationStatus.Read]      = [],
        [NotificationStatus.Failed]    = []
    };

    private readonly INotificationRepository _notificationRepo;

    public StatusTracker(INotificationRepository notificationRepo) =>
        _notificationRepo = notificationRepo;

    public void Transition(int notificationId, NotificationStatus newStatus)
    {
        var notification = _notificationRepo.GetById(notificationId)
            ?? throw new KeyNotFoundException($"Notification {notificationId} not found.");

        if (!_validTransitions[notification.Status].Contains(newStatus))
            throw new InvalidOperationException(
                $"Invalid transition: {notification.Status} → {newStatus}");

        var oldStatus = notification.Status;
        _notificationRepo.UpdateStatus(notificationId, newStatus);
        Console.WriteLine($"[StatusTracker] Notification {notificationId}: {oldStatus} → {newStatus}");
    }
}
