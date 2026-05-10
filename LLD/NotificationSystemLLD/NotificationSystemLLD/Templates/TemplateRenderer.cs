using System.Text.RegularExpressions;
using NotificationSystemLLD.Domain.Models;
using NotificationSystemLLD.Interfaces;

namespace NotificationSystemLLD.Templates;

// Rendering flow:
// 1. Fetch template by (NotificationType, Channel) from ITemplateRepository
// 2. Replace {{key}} tokens with Notification.Payload values
// 3. Store rendered string back into Notification.RenderedBody
public class TemplateRenderer : ITemplateRenderer
{
    private readonly ITemplateRepository _templateRepo;

    public TemplateRenderer(ITemplateRepository templateRepo) =>
        _templateRepo = templateRepo;

    public string Render(Notification notification)
    {
        var template = _templateRepo.Get(notification.NotificationType, notification.Channel)
            ?? throw new InvalidOperationException(
                $"No template for {notification.NotificationType} / {notification.Channel}");

        // Render both body and subject (subject used by EmailChannel)
        notification.RenderedBody = ReplacePlaceholders(template.Body, notification.Payload);
        notification.TemplateId   = template.TemplateId;

        // Store rendered subject back into payload so EmailChannel can access it
        if (!string.IsNullOrEmpty(template.Subject))
            notification.Payload["_subject"] = ReplacePlaceholders(template.Subject, notification.Payload);

        return notification.RenderedBody;
    }

    private static string ReplacePlaceholders(string text, Dictionary<string, string> payload) =>
        Regex.Replace(text, @"\{\{(\w+)\}\}", m =>
            payload.TryGetValue(m.Groups[1].Value, out var val) ? val : m.Value);
}
