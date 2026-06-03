using NotificationSystemLLD.Domain.Enums;

namespace NotificationSystemLLD.Domain.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string DeviceToken { get; set; } = string.Empty;       // for Push
    public List<Subscription> Subscriptions { get; set; } = [];
    public NotificationPreference Preference { get; set; } = new();
}
