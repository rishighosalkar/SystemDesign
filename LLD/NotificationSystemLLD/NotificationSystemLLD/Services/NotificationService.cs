using NotificationSystemLLD.Domain.Enums;
using NotificationSystemLLD.Domain.Models;
using NotificationSystemLLD.Interfaces;

namespace NotificationSystemLLD.Services;

// Orchestrator — ties the entire flow together.
// Flow: Receive event → Filter channels → Render template → Enqueue → (Dispatcher picks up)
public class NotificationService
{
    private readonly IUserRepository _userRepo;
    private readonly IUserPreferenceService _preferenceService;
    private readonly ITemplateRenderer _renderer;
    private readonly IMessageQueue _queue;
    private readonly INotificationRepository _notificationRepo;

    public NotificationService(
        IUserRepository userRepo,
        IUserPreferenceService preferenceService,
        ITemplateRenderer renderer,
        IMessageQueue queue,
        INotificationRepository notificationRepo)
    {
        _userRepo          = userRepo;
        _preferenceService = preferenceService;
        _renderer          = renderer;
        _queue             = queue;
        _notificationRepo  = notificationRepo;
    }

    // Entry point: called by any event source (Order Service, Payment Service, etc.)
    public void Send(int userId, NotificationType type, NotificationPriority priority,
                     Dictionary<string, string> payload)
    {
        var user = _userRepo.GetById(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        var activeChannels = _preferenceService.GetActiveChannels(user, type).ToList();

        if (activeChannels.Count == 0)
        {
            Console.WriteLine($"[NotificationService] No active channels for user {userId} / {type}");
            return;
        }

        foreach (var channel in activeChannels)
        {
            var notification = new Notification
            {
                UserId           = userId,
                NotificationType = type,
                Priority         = priority,
                Channel          = channel,
                Payload          = payload,
                Status           = NotificationStatus.Queued
            };

            // Render template — fills RenderedBody and TemplateId
            _renderer.Render(notification);

            _notificationRepo.Save(notification);
            _queue.Enqueue(notification);

            Console.WriteLine($"[NotificationService] Queued → User:{userId} | {type} | {channel} | Id:{notification.NotificationId}");
        }
    }

    public NotificationStatus? GetStatus(int notificationId) =>
        _notificationRepo.GetById(notificationId)?.Status;
}
