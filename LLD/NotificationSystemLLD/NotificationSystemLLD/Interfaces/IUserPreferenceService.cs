using NotificationSystemLLD.Domain.Enums;
using NotificationSystemLLD.Domain.Models;

namespace NotificationSystemLLD.Interfaces;

// Answers: "Which channels should this notification go to for this user?"
// Two-level filter:
//   Level 1 — Subscription: is user subscribed to the category this event belongs to?
//   Level 2 — Preference:   which channels has the user globally enabled?
public interface IUserPreferenceService
{
    IEnumerable<Channel> GetActiveChannels(User user, NotificationType notificationType);
    void Subscribe(int userId, NotificationCategory category);
    void Unsubscribe(int userId, NotificationCategory category);
    void UpdateChannelOptIn(int userId, Channel channel, bool optIn);
}
