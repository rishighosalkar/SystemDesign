namespace NotificationSystemLLD.Domain.Enums;

// Broad category a user subscribes to.
// Subscribing to a category means receiving ALL event types within it.
// e.g. OrderUpdates → OrderPlaced, OrderShipped, OrderDelivered, OrderCancelled
public enum NotificationCategory
{
    OrderUpdates,
    PaymentAlerts,
    Otp
}
