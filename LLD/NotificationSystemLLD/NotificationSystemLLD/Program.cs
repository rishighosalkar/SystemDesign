using NotificationSystemLLD.Channels;
using NotificationSystemLLD.Channels.Vendors;
using NotificationSystemLLD.Domain.Enums;
using NotificationSystemLLD.Domain.Models;
using NotificationSystemLLD.Infrastructure.Queue;
using NotificationSystemLLD.Infrastructure.Repositories;
using NotificationSystemLLD.Infrastructure.Retry;
using NotificationSystemLLD.Services;
using NotificationSystemLLD.Templates;

// ─── Repositories ────────────────────────────────────────────────────────────
var userRepo         = new InMemoryUserRepository();
var templateRepo     = new InMemoryTemplateRepository();
var notificationRepo = new InMemoryNotificationRepository();

// ─── Seed a user ─────────────────────────────────────────────────────────────
// Alice has subscribed to OrderUpdates (one subscription covers all order events).
// She has Email + SMS enabled, Push disabled globally via preferences.
var user = new User
{
    Id          = 1,
    Name        = "Alice",
    Email       = "<alice@example.com>",
    PhoneNumber = "<+1-555-000-0001>",
    DeviceToken = "<device-token-abc123>",
    Preference  = new NotificationPreference
    {
        UserId      = 1,
        ChannelOptIn = new()
        {
            [Channel.Email] = true,
            [Channel.Sms]   = true,
            [Channel.Push]  = false,   // Alice has Push globally disabled
            [Channel.InApp] = true
        }
    },
    // One subscription covers OrderPlaced, OrderShipped, OrderDelivered, OrderCancelled
    Subscriptions =
    [
        new() { SubscriptionId = 1, UserId = 1, Category = NotificationCategory.OrderUpdates, IsActive = true },
        new() { SubscriptionId = 2, UserId = 1, Category = NotificationCategory.Otp,          IsActive = true },
    ]
};
userRepo.Save(user);

// ─── Infrastructure ───────────────────────────────────────────────────────────
var queue       = new InMemoryMessageQueue();
var retryPolicy = new ExponentialBackoffRetryPolicy(maxRetries: 3, baseDelaySeconds: 1);

// ─── Channels (Strategy implementations) ─────────────────────────────────────
var emailChannel = new EmailChannel(new EmailVendor());
var smsChannel   = new SmsChannel(new SmsVendor());
var pushChannel  = new PushChannel(new PushVendor());
var inAppChannel = new InAppChannel(new InAppVendor());

// ─── Factory ──────────────────────────────────────────────────────────────────
var channelFactory = new NotificationChannelFactory(
    [emailChannel, smsChannel, pushChannel, inAppChannel]);

// ─── Services ─────────────────────────────────────────────────────────────────
var preferenceService = new NotificationSystemLLD.Services.UserPreferenceService(userRepo);
var renderer          = new TemplateRenderer(templateRepo);
var statusTracker     = new StatusTracker(notificationRepo);
var dispatcher        = new ChannelDispatcher(queue, userRepo, channelFactory, retryPolicy, statusTracker);

var notificationService = new NotificationSystemLLD.Services.NotificationService(
    userRepo, preferenceService, renderer, queue, notificationRepo);

// ─── Sample Flow: ORDER_DELIVERED notification ────────────────────────────────
// Alice subscribed to OrderUpdates → she gets notified for OrderDelivered.
// Push is disabled globally → only Email, SMS, InApp are sent.
Console.WriteLine("=== ORDER_DELIVERED Event Received ===\n");

notificationService.Send(
    userId:   1,
    type:     NotificationType.OrderDelivered,
    priority: NotificationPriority.High,
    payload:  new Dictionary<string, string>
    {
        ["name"]    = "Alice",
        ["orderId"] = "ORD-9921",
        ["date"]    = "2025-07-10"
    });

Console.WriteLine("\n=== Dispatcher Processing Queue ===\n");
await dispatcher.ProcessQueueAsync();

Console.WriteLine("\n=== Notification Statuses ===");
for (int id = 1; id <= 3; id++)
{
    var status = notificationService.GetStatus(id);
    if (status.HasValue)
        Console.WriteLine($"  Notification {id}: {status}");
}
