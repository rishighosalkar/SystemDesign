using NotificationSystemLLD.Domain.Enums;

namespace NotificationSystemLLD.Domain.Models;

// Represents a user's opt-in for a notification category.
// Subscribing to a category means receiving ALL event types within it.
// e.g. User subscribes to OrderUpdates → gets OrderPlaced, OrderShipped, OrderDelivered, OrderCancelled
// Which channels to use is controlled separately by NotificationPreference.
public class Subscription
{
    public int SubscriptionId { get; set; }
    public int UserId { get; set; }
    public NotificationCategory Category { get; set; }
    public bool IsActive { get; set; } = true;
}
