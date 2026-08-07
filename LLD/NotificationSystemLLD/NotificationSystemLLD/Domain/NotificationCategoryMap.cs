using NotificationSystemLLD.Domain.Enums;

namespace NotificationSystemLLD.Domain;

// Maps each NotificationType to its parent NotificationCategory.
// Used by UserPreferenceService to check if a user is subscribed to the category
// that the incoming event belongs to.
public static class NotificationCategoryMap
{
    private static readonly Dictionary<NotificationType, NotificationCategory> _map = new()
    {
        { NotificationType.OrderPlaced,    NotificationCategory.OrderUpdates  },
        { NotificationType.OrderShipped,   NotificationCategory.OrderUpdates  },
        { NotificationType.OrderDelivered, NotificationCategory.OrderUpdates  },
        { NotificationType.OrderCancelled, NotificationCategory.OrderUpdates  },
        { NotificationType.PaymentFailed,  NotificationCategory.PaymentAlerts },
        { NotificationType.PaymentSuccess, NotificationCategory.PaymentAlerts },
        { NotificationType.Otp,            NotificationCategory.Otp           },
    };

    public static NotificationCategory GetCategory(NotificationType type) =>
        _map.TryGetValue(type, out var category)
            ? category
            : throw new KeyNotFoundException($"No category mapped for NotificationType '{type}'.");
}
