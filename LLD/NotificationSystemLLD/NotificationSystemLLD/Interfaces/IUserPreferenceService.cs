using NotificationSystemLLD.Domain.Enums;
using NotificationSystemLLD.Domain.Models;

namespace NotificationSystemLLD.Interfaces;

// Answers: "Which channels should this notification actually go to for this user?"
// Combines subscription check + global channel opt-in check.
public interface IUserPreferenceService
{
    IEnumerable<Channel> GetActiveChannels(User user, NotificationType notificationType);
    void Subscribe(int userId, NotificationType type, Channel channel);
    void Unsubscribe(int userId, NotificationType type, Channel channel);
    void UpdateChannelOptIn(int userId, Channel channel, bool optIn);
}
