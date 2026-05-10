using NotificationSystemLLD.Domain.Enums;
using NotificationSystemLLD.Domain.Models;
using NotificationSystemLLD.Interfaces;

namespace NotificationSystemLLD.Services;

// Two-level filter:
// Level 1 — Subscription: has the user subscribed to this NotificationType on this Channel?
// Level 2 — Global opt-in: has the user globally enabled this Channel?
public class UserPreferenceService : IUserPreferenceService
{
    private readonly IUserRepository _userRepo;

    public UserPreferenceService(IUserRepository userRepo) => _userRepo = userRepo;

    public IEnumerable<Channel> GetActiveChannels(User user, NotificationType notificationType) =>
        user.Subscriptions
            .Where(s => s.NotificationType == notificationType
                     && s.IsActive
                     && user.Preference.ChannelOptIn.GetValueOrDefault(s.Channel, false))
            .Select(s => s.Channel)
            .Distinct();

    public void Subscribe(int userId, NotificationType type, Channel channel)
    {
        var user = GetUserOrThrow(userId);
        var existing = user.Subscriptions
            .FirstOrDefault(s => s.NotificationType == type && s.Channel == channel);

        if (existing is not null) existing.IsActive = true;
        else user.Subscriptions.Add(new Subscription
        {
            UserId           = userId,
            NotificationType = type,
            Channel          = channel,
            IsActive         = true
        });
        _userRepo.Save(user);
    }

    public void Unsubscribe(int userId, NotificationType type, Channel channel)
    {
        var user = GetUserOrThrow(userId);
        var sub  = user.Subscriptions
            .FirstOrDefault(s => s.NotificationType == type && s.Channel == channel);
        if (sub is not null) sub.IsActive = false;
        _userRepo.Save(user);
    }

    public void UpdateChannelOptIn(int userId, Channel channel, bool optIn)
    {
        var user = GetUserOrThrow(userId);
        user.Preference.ChannelOptIn[channel] = optIn;
        _userRepo.Save(user);
    }

    private User GetUserOrThrow(int userId) =>
        _userRepo.GetById(userId) ?? throw new KeyNotFoundException($"User {userId} not found.");
}
