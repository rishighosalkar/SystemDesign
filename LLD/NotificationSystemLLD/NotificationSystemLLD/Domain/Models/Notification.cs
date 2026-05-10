using NotificationSystemLLD.Domain.Enums;

namespace NotificationSystemLLD.Domain.Models;

public class Notification
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
    public NotificationType NotificationType { get; set; }
    public NotificationPriority Priority { get; set; }
    public Channel Channel { get; set; }
    public Dictionary<string, string> Payload { get; set; } = [];  // template variables
    public string TemplateId { get; set; } = string.Empty;
    public string RenderedBody { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; } = NotificationStatus.Queued;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int RetryCount { get; set; } = 0;
}
