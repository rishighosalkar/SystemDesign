using NotificationSystemLLD.Domain.Enums;

namespace NotificationSystemLLD.Domain.Models;

// Templates are channel-specific. EMAIL has Subject+Body, SMS has only Body.
// Body uses {{placeholder}} syntax: "Hello {{name}}, your order {{orderId}} is delivered."
public class MessageTemplate
{
    public string TemplateId { get; set; } = string.Empty;
    public Channel Channel { get; set; }
    public NotificationType NotificationType { get; set; }
    public string Subject { get; set; } = string.Empty;   // relevant for Email
    public string Body { get; set; } = string.Empty;
}
