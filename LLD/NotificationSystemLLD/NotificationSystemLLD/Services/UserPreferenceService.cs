using NotificationSystemLLD.Domain;
using NotificationSystemLLD.Domain.Enums;
using NotificationSystemLLD.Domain.Models;
using NotificationSystemLLD.Interfaces;

namespace NotificationSystemLLD.Services;

// Two-level filter:
// Level 1 — Subscription: is user subscribed to the category this event belongs to?
//            e.g. OrderDelivered belongs to OrderUpdates — is user subscribed to OrderUpdates?
// Level 2 — Preference: which channels has the user globally enabled?
//            e.g. user enabled Email + Push, disabled SMS → only Email + Push are returned
public class UserPreferenceService : IUserPreferenceService
{
    private readonly IUserRepository _userRepo;

    public UserPreferenceService(IUserRepository userRepo) => _userRepo = userRepo;

    public IEnumerable<Channel> GetActiveChannels(User user, NotificationType notificationType)
    {
        var category = NotificationCategoryMap.GetCategory(notificationType);

        // Level 1: is user subscribed to this category?
        var isSubscribed = user.Subscriptions
            .Any(s => s.Category == category && s.IsActive);

        if (!isSubscribed)
            return [];

        // Level 2: return only channels the user has globally enabled
        return user.Preference.ChannelOptIn
            .Where(kv => kv.Value)
            .Select(kv => kv.Key);
    }

    public void Subscribe(int userId, NotificationCategory category)
    {
        var user = GetUserOrThrow(userId);
        var existing = user.Subscriptions.FirstOrDefault(s => s.Category == category);

        if (existing is not null) existing.IsActive = true;
        else user.Subscriptions.Add(new Subscription
        {
            UserId   = userId,
            Category = category,
            IsActive = true
        });
        _userRepo.Save(user);
    }

    public void Unsubscribe(int userId, NotificationCategory category)
    {
        var user = GetUserOrThrow(userId);
        var sub  = user.Subscriptions.FirstOrDefault(s => s.Category == category);
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
