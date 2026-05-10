using NotificationSystemLLD.Domain.Enums;

namespace NotificationSystemLLD.Domain.Models;

// Global channel-level opt-in/out for a user.
// e.g. User has globally disabled SMS — no SMS regardless of subscriptions.
public class NotificationPreference
{
    public int UserId { get; set; }
    public Dictionary<Channel, bool> ChannelOptIn { get; set; } = new()
    {
        { Channel.Email, true },
        { Channel.Sms,   true },
        { Channel.Push,  true },
        { Channel.InApp, true }
    };
}
