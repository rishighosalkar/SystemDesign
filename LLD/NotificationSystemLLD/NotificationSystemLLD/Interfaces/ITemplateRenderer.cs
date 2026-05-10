using NotificationSystemLLD.Domain.Models;

namespace NotificationSystemLLD.Interfaces;

// Resolves the correct template for (NotificationType, Channel) and
// replaces {{placeholders}} with actual payload values.
public interface ITemplateRenderer
{
    string Render(Notification notification);
}
