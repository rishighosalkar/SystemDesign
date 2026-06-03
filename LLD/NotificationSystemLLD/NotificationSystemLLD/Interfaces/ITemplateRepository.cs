using NotificationSystemLLD.Domain.Enums;
using NotificationSystemLLD.Domain.Models;

namespace NotificationSystemLLD.Interfaces;

public interface ITemplateRepository
{
    MessageTemplate? Get(NotificationType type, Channel channel);
}
