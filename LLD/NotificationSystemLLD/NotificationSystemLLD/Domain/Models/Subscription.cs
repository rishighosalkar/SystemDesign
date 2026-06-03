using NotificationSystemLLD.Domain.Enums;

namespace NotificationSystemLLD.Domain.Models;

// Represents a user's opt-in for a specific NotificationType on a specific Channel.
// e.g. User 42 wants ORDER_DELIVERED via EMAIL and SMS.
public class Subscription
{
    public int SubscriptionId { get; set; }
    public int UserId { get; set; }
    public NotificationType NotificationType { get; set; }
    public Channel Channel { get; set; }
    public bool IsActive { get; set; } = true;
}
