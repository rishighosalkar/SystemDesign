namespace NotificationSystemLLD.Domain.Enums;

// Specific event types fired by upstream services.
// Each type belongs to a NotificationCategory.
// OrderPlaced, OrderShipped, OrderDelivered, OrderCancelled → OrderUpdates
// PaymentFailed, PaymentSuccess                             → PaymentAlerts
// Otp                                                       → Otp
public enum NotificationType
{
    OrderPlaced,
    OrderShipped,
    OrderDelivered,
    OrderCancelled,
    PaymentFailed,
    PaymentSuccess,
    Otp
}
